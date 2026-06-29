"""
Run this FIRST, before the Gemini server or Unity.

    pip install google-genai
    set GEMINI_API_KEY=your-key-here          (Windows; get one free at aistudio.google.com/apikey)
    python smoke_test_gemini.py

A sensible shopkeeper reply means your key + model are correct and every later
problem is the server or Unity, not the model call.
"""
import os
from google import genai
from google.genai import types

client = genai.Client(api_key=os.environ["GEMINI_API_KEY"])

resp = client.models.generate_content(
    model="gemini-2.5-flash",
    contents="Hello, what do you sell?",
    config=types.GenerateContentConfig(
        system_instruction="You are a cheerful shopkeeper in a small fantasy town. Stay in character.",
        max_output_tokens=300,
    ),
)
print(resp.text)
