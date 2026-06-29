"""
Phase 1 relay server — GEMINI (free tier) version.

This is a DROP-IN replacement for the Anthropic server.py. The /generate endpoint
keeps the EXACT same request/response shape, so nothing in Unity changes. Switching
back to Anthropic later means swapping this one file back — the IDialogueProvider
abstraction means the game never knows or cares which model answered.

Setup:
    pip install fastapi uvicorn google-genai
    Get a free key (no card) at https://aistudio.google.com/apikey
    set GEMINI_API_KEY=your-key-here          (Windows, same window as uvicorn)
    uvicorn server_gemini:app --reload --port 8000

Free tier reality (Gemini 2.5 Flash): ~10-15 requests/minute, ~1,500/day, no expiry.
The per-minute limit is the one you'll feel; the backoff below handles brief hits.
Note: on the free tier Google may use prompts to improve their products.
"""
import os
import time
from typing import List, Literal
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from google import genai
from google.genai import types
from google.genai.errors import ClientError

client = genai.Client(api_key=os.environ["GEMINI_API_KEY"])
MODEL = "gemini-2.5-flash"     # free-tier workhorse. Flash-Lite = higher limits, lower quality.

app = FastAPI()
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"], allow_methods=["*"], allow_headers=["*"],
)


class Message(BaseModel):
    role: Literal["user", "assistant"]
    content: str


class GenerateRequest(BaseModel):
    system: str = ""
    messages: List[Message]
    max_tokens: int = 400


class GenerateResponse(BaseModel):
    reply: str
    input_tokens: int = 0
    output_tokens: int = 0


def _to_gemini_contents(messages: List[Message]):
    # Gemini uses role "model" for the assistant and wraps text in parts.
    contents = []
    for m in messages:
        role = "model" if m.role == "assistant" else "user"
        contents.append(types.Content(role=role, parts=[types.Part(text=m.content)]))
    return contents


@app.post("/generate", response_model=GenerateResponse)
def generate(req: GenerateRequest) -> GenerateResponse:
    config = types.GenerateContentConfig(
        system_instruction=req.system or None,   # the character context lives here
        max_output_tokens=req.max_tokens,
    )

    # Simple backoff so a brief per-minute rate-limit hit waits instead of erroring.
    last_err = None
    for attempt in range(3):
        try:
            resp = client.models.generate_content(
                model=MODEL,
                contents=_to_gemini_contents(req.messages),
                config=config,
            )
            text = resp.text or ""
            usage = resp.usage_metadata
            return GenerateResponse(
                reply=text,
                input_tokens=getattr(usage, "prompt_token_count", 0) or 0,
                output_tokens=getattr(usage, "candidates_token_count", 0) or 0,
            )
        except ClientError as e:
            last_err = e
            # 429 = rate limited. Wait a few seconds and retry.
            if getattr(e, "status_code", None) == 429 and attempt < 2:
                time.sleep(5 * (attempt + 1))
                continue
            raise
    raise last_err


@app.get("/health")
def health():
    return {"ok": True, "model": MODEL}
