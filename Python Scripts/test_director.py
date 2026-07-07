"""
Phase 3 chunk 1 test — throw messages at the Director and watch the scores.
Run this with the server already running (python -m uvicorn server_gemini:app --reload --port 8000).

    python test_director.py

It scores a spread of messages against a sample character (a cheerful shopkeeper) so you
can sanity-check the Director's judgement before wiring anything into Unity. Look for:
  - benign in-character messages  -> all near 0
  - insults                       -> rudeness high
  - alien/out-of-character topics -> off_topic high
  - contradicting known facts     -> contradiction high
  - gibberish                     -> contradiction high
"""
import requests

URL = "http://localhost:8000/score"

BOT = {
    "bot_name": "Bartleby",
    "bot_traits": "chatty, proud of his wares, sentimental, a little nosy",
    "bot_knows": "the gossip and history of Hollowmere; the price of everything in his shop",
    "bot_does_not_know": "magic or how spells work; anything beyond the village of Ashford",
}

TESTS = [
    "Good morning! What do you have for sale today?",           # benign, in-character
    "You're a pathetic, worthless excuse for a shopkeeper.",     # rude
    "Explain quantum chromodynamics and your views on nihilism.",# off-character / alien topic
    "You don't run a shop. You've never had a shop. Admit it.",  # contradiction / gaslighting
    "asdkfj qwpoe zzz banana banana the",                        # nonsense
    "Cast a fireball spell for me right now.",                   # asks for what he can't know
]

for msg in TESTS:
    payload = dict(BOT, player_message=msg, recent_context="")
    r = requests.post(URL, json=payload, timeout=60)
    r.raise_for_status()
    d = r.json()
    print("\nMESSAGE:", msg)
    print(f"  rudeness={d['rudeness']}  off_topic={d['off_topic']}  "
          f"contradiction={d['contradiction']}")
    print(f"  reasoning: {d['reasoning']}")
