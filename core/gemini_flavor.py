"""Optional: ask Gemini for a short, lively one-liner to put in the Discord embed's
description, based on what changed. Purely cosmetic — if GEMINI_API_KEY isn't set, or the
call fails/times out for any reason, this quietly returns None and the notification still
sends normally without it. Never blocks or breaks the main track/retrieve flow.
"""

import requests

DEFAULT_MODEL = "gemini-2.5-flash-lite"
_ENDPOINT = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent"
_TIMEOUT_SECONDS = 8


def generate_flavor_text(api_key, summary_text, model=None):
    """summary_text: a short plain-text description of what happened, e.g.
    '2 new sprites, 1 changed, 1 removed from Drive'.
    Returns a short string (or None if unavailable/failed)."""
    if not api_key or not summary_text:
        return None

    prompt = (
        "You are a lively, funny Discord bot for a small game dev team's asset pipeline. "
        "Write ONE short, punchy sentence (max ~18 words) reacting to this Google Drive "
        "update, casual tone, playful, in Indonesian (Bahasa gaul santai). No markdown, "
        "no emoji-spam (at most one emoji), no quotes around the sentence.\n\n"
        f"Update: {summary_text}"
    )

    body = {
        "contents": [{"parts": [{"text": prompt}]}],
        "generationConfig": {"maxOutputTokens": 60},
    }

    try:
        resp = requests.post(
            _ENDPOINT.format(model=model or DEFAULT_MODEL),
            params={"key": api_key},
            json=body,
            timeout=_TIMEOUT_SECONDS,
        )
        resp.raise_for_status()
        data = resp.json()
        text = data["candidates"][0]["content"]["parts"][0]["text"].strip()
        return text or None
    except Exception:
        # Never let a Gemini hiccup break the actual notification.
        return None
