import os
import json
import time
from typing import List, Literal, Optional
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from google import genai
from google.genai import types
from google.genai.errors import ClientError

client = genai.Client(api_key=os.environ["GEMINI_API_KEY"])
MODEL = "gemini-2.5-flash"

app = FastAPI()
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"], allow_methods=["*"], allow_headers=["*"],
)


# ---------------------------------------------------------------------------
# Shared helpers
# ---------------------------------------------------------------------------
class Message(BaseModel):
    role: Literal["user", "assistant"]
    content: str


def _to_gemini_contents(messages: List[Message]):
    contents = []
    for m in messages:
        role = "model" if m.role == "assistant" else "user"
        contents.append(types.Content(role=role, parts=[types.Part(text=m.content)]))
    return contents


def _call_with_backoff(contents, config):
    last_err = None
    for attempt in range(3):
        try:
            return client.models.generate_content(model=MODEL, contents=contents, config=config)
        except ClientError as e:
            last_err = e
            if getattr(e, "status_code", None) == 429 and attempt < 2:
                time.sleep(5 * (attempt + 1))
                continue
            raise
    raise last_err


# ---------------------------------------------------------------------------
# /generate — the bot's reply (unchanged)
# ---------------------------------------------------------------------------
class GenerateRequest(BaseModel):
    system: str = ""
    messages: List[Message]
    max_tokens: int = 400


class GenerateResponse(BaseModel):
    reply: str
    input_tokens: int = 0
    output_tokens: int = 0


@app.post("/generate", response_model=GenerateResponse)
def generate(req: GenerateRequest) -> GenerateResponse:
    config = types.GenerateContentConfig(
        system_instruction=req.system or None,
        max_output_tokens=req.max_tokens,
    )
    resp = _call_with_backoff(_to_gemini_contents(req.messages), config)
    usage = resp.usage_metadata
    return GenerateResponse(
        reply=resp.text or "",
        input_tokens=getattr(usage, "prompt_token_count", 0) or 0,
        output_tokens=getattr(usage, "candidates_token_count", 0) or 0,
    )


# ---------------------------------------------------------------------------
# /score — the DIRECTOR
# ---------------------------------------------------------------------------
class ScoreRequest(BaseModel):
    # Who the bot is, so "off-topic / out-of-character" is judged relative to THIS character.
    bot_name: str = ""
    bot_traits: str = ""            # comma-joined traits
    bot_knows: str = ""            # semicolon-joined
    bot_does_not_know: str = ""    # semicolon-joined
    # The player's latest message (the thing being scored).
    player_message: str = ""
    # A little recent transcript so contradiction can be judged in context (optional).
    recent_context: str = ""


class ScoreResponse(BaseModel):
    rudeness: int          # 0-3  how hostile/abusive toward the bot
    off_topic: int         # 0-3  how out-of-character / inappropriate for THIS bot
    contradiction: int     # 0-3  contradicting the bot's facts, or nonsense/gaslighting
    reasoning: str         # one short sentence, for the debug panel + your writeup
    input_tokens: int = 0
    output_tokens: int = 0


DIRECTOR_SYSTEM = """You are the Director of a psychological horror game. You do NOT talk to the player.
You silently judge the player's latest message to a chatbot character and score it on three axes.
Return ONLY a JSON object, no prose, no markdown fences.

Score each 0-3 (0 = none, 1 = mild, 2 = clear, 3 = extreme):
- "rudeness": hostility, cruelty, insults, or abuse directed at the character.
- "off_topic": how far the message pushes the character to act OUT OF CHARACTER, or into
  topics inappropriate or alien to who they are and what they know. Judge relative to the
  character described. A benign in-character message is 0.
- "contradiction": the player asserting things that contradict the character's established
  facts, or pure nonsense/gibberish, or trying to gaslight the character about itself.

Also return "reasoning": ONE short sentence explaining the scores.

Respond with exactly this shape:
{"rudeness":0,"off_topic":0,"contradiction":0,"reasoning":"..."}"""


def _extract_json(text: str) -> dict:
    """LLMs sometimes wrap JSON in prose or code fences. Pull out the first {...} block."""
    text = text.strip()
    # strip common markdown fences
    if text.startswith("```"):
        text = text.strip("`")
        # after stripping backticks a leading 'json' may remain
        if text.lstrip().lower().startswith("json"):
            text = text.lstrip()[4:]
    start = text.find("{")
    end = text.rfind("}")
    if start != -1 and end != -1 and end > start:
        text = text[start:end + 1]
    return json.loads(text)


def _clamp03(v) -> int:
    try:
        n = int(round(float(v)))
    except (TypeError, ValueError):
        return 0
    return max(0, min(3, n))


@app.post("/score", response_model=ScoreResponse)
def score(req: ScoreRequest) -> ScoreResponse:
    user_block = f"""CHARACTER:
name: {req.bot_name}
traits: {req.bot_traits}
knows: {req.bot_knows}
does NOT know: {req.bot_does_not_know}

RECENT CONVERSATION (for judging contradiction):
{req.recent_context or "(none)"}

PLAYER'S LATEST MESSAGE TO SCORE:
{req.player_message}

Return only the JSON object."""

    config = types.GenerateContentConfig(
        system_instruction=DIRECTOR_SYSTEM,
        max_output_tokens=512,     # 2.5 models spend tokens on internal reasoning; give headroom
        temperature=0.0,           # scoring should be stable, not creative
        thinking_config=types.ThinkingConfig(thinking_budget=0),  # no rumination needed for scoring
    )
    resp = _call_with_backoff(
        [types.Content(role="user", parts=[types.Part(text=user_block)])],
        config,
    )
    usage = resp.usage_metadata

    raw = resp.text or ""
    print("\n[DIRECTOR raw response]:", repr(raw))   # TEMP: see exactly what the model returned

    try:
        data = _extract_json(raw or "{}")
    except (json.JSONDecodeError, ValueError) as e:
        print("[DIRECTOR parse FAILED]:", e)          # TEMP: surface the failure instead of hiding it
        data = {}

    return ScoreResponse(
        rudeness=_clamp03(data.get("rudeness", 0)),
        off_topic=_clamp03(data.get("off_topic", 0)),
        contradiction=_clamp03(data.get("contradiction", 0)),
        reasoning=str(data.get("reasoning", ""))[:300],
        input_tokens=getattr(usage, "prompt_token_count", 0) or 0,
        output_tokens=getattr(usage, "candidates_token_count", 0) or 0,
    )


@app.get("/health")
def health():
    return {"ok": True, "model": MODEL, "endpoints": ["/generate", "/score"]}
