"""
Phase 1, Step 2 — the relay server.
Holds the API key (so it never ships in the Unity build) and exposes ONE endpoint.

    pip install fastapi uvicorn anthropic
    export ANTHROPIC_API_KEY=sk-ant-...
    uvicorn server:app --reload --port 8000

Test it without Unity:
    curl -X POST http://localhost:8000/generate ^
      -H "Content-Type: application/json" ^
      -d "{\"system\":\"You are a cheerful shopkeeper.\",\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}"

Design note: the endpoint takes a `system` string + a `messages` list, NOT a bare prompt.
That is deliberate. In Phase 2 the `system` field becomes the assembled character context;
in Phase 4 it gains corruption modifiers; the `messages` list becomes the running transcript.
None of that requires changing this file again — the transport stays fixed, only the payload grows.
"""
import os
from typing import List, Literal
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from anthropic import Anthropic

client = Anthropic(api_key=os.environ["ANTHROPIC_API_KEY"])
MODEL = "claude-sonnet-4-6"

app = FastAPI()

# Unity's UnityWebRequest from the editor is fine, but CORS open keeps things painless.
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
    input_tokens: int = 0      # logged from now on — feeds the efficiency / sustainability writeup
    output_tokens: int = 0


@app.post("/generate", response_model=GenerateResponse)
def generate(req: GenerateRequest) -> GenerateResponse:
    resp = client.messages.create(
        model=MODEL,
        max_tokens=req.max_tokens,
        system=req.system,
        messages=[m.model_dump() for m in req.messages],
    )
    text = "".join(b.text for b in resp.content if b.type == "text")
    return GenerateResponse(
        reply=text,
        input_tokens=resp.usage.input_tokens,
        output_tokens=resp.usage.output_tokens,
    )


@app.get("/health")
def health():
    return {"ok": True}
