"""
Phase 1, Step 1 — prove we can talk to Claude at all.
Run this BEFORE building the server or touching Unity.

    pip install anthropic
    export ANTHROPIC_API_KEY=sk-ant-...      (Windows: set ANTHROPIC_API_KEY=...)
    python smoke_test.py

If it prints a sensible reply, your key + model string + request shape are all correct,
and every later problem is isolated to the server or Unity, not the model call.
"""
import os
from anthropic import Anthropic

client = Anthropic(api_key=os.environ["ANTHROPIC_API_KEY"])

resp = client.messages.create(
    model="claude-sonnet-4-6",          # fast + cheap enough for a per-turn demo; swap later if needed
    max_tokens=300,
    system="You are a cheerful shopkeeper in a small fantasy town. Stay in character.",
    messages=[{"role": "user", "content": "Hello, what do you sell?"}],
)

# resp.content is a list of blocks; for plain text we want the text blocks joined.
text = "".join(block.text for block in resp.content if block.type == "text")
print(text)
