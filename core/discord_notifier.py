"""Send notifications to a Discord webhook. Kept separate from the Drive logic so it can be
used both by track.py (change report) and retrieve.py (download result report)."""

import requests

from core import gemini_flavor


def _post(webhook_url, embed):
    r = requests.post(webhook_url, json={"embeds": [embed]}, timeout=15)
    r.raise_for_status()


def _fmt(files, limit=15):
    lines = [f"- [{f['relative_path']}]({f.get('webViewLink', '')})" for f in files[:limit]]
    if len(files) > limit:
        lines.append(f"... and {len(files) - limit} more")
    return "\n".join(lines) if lines else "_none_"


def _fmt_names(names, limit=15):
    lines = [f"- {n}" for n in names[:limit]]
    if len(names) > limit:
        lines.append(f"... and {len(names) - limit} more")
    return "\n".join(lines) if lines else "_none_"


def send_track_notification(
    webhook_url, pair_name, new_files, changed_files, deleted_files,
    gemini_api_key=None, gemini_model=None,
):
    """Report the result of a change check. Not sent if there are no changes at all."""
    if not (new_files or changed_files or deleted_files):
        return

    fields = []
    if new_files:
        fields.append({"name": f"🆕 New files ({len(new_files)})", "value": _fmt(new_files), "inline": False})
    if changed_files:
        fields.append({"name": f"✏️ Changed files ({len(changed_files)})", "value": _fmt(changed_files), "inline": False})
    if deleted_files:
        fields.append({
            "name": f"🗑️ Removed from Drive ({len(deleted_files)})",
            "value": _fmt(deleted_files) + "\n_(not deleted automatically anywhere)_",
            "inline": False,
        })

    embed = {
        "title": f"📁 Drive update — {pair_name}",
        "color": 0x4285F4,
        "fields": fields,
    }

    summary = f"{len(new_files)} new, {len(changed_files)} changed, {len(deleted_files)} removed"
    flavor = gemini_flavor.generate_flavor_text(gemini_api_key, summary, gemini_model)
    if flavor:
        embed["description"] = flavor

    _post(webhook_url, embed)


def send_retrieve_notification(
    webhook_url, pair_name, retrieved_names, updated_names, skipped_names,
    gemini_api_key=None, gemini_model=None,
):
    """Report the result of a retrieve/download run. Always fires when there's something to
    report (something downloaded, updated, and/or skipped). webhook_url is required by
    retrieve.py.

    - retrieved_names: brand-new files downloaded for the first time.
    - updated_names: files that existed before and got re-downloaded because the SAME Drive
      file changed (overwrote the local copy).
    - skipped_names: brand-new Drive files whose name collided with some OTHER local file.
    """
    if not (retrieved_names or updated_names or skipped_names):
        return

    fields = []
    if retrieved_names:
        fields.append({
            "name": f"⬇️ Downloaded ({len(retrieved_names)})",
            "value": _fmt_names(retrieved_names),
            "inline": False,
        })
    if updated_names:
        fields.append({
            "name": f"🔄 Updated, file changed in Drive ({len(updated_names)})",
            "value": _fmt_names(updated_names),
            "inline": False,
        })
    if skipped_names:
        fields.append({
            "name": f"⏭️ Skipped, name already exists ({len(skipped_names)})",
            "value": _fmt_names(skipped_names),
            "inline": False,
        })

    embed = {
        "title": f"⬇️ Retrieve result — {pair_name}",
        "color": 0x57F287,
        "fields": fields,
    }

    summary = f"{len(retrieved_names)} downloaded, {len(updated_names)} updated, {len(skipped_names)} skipped"
    flavor = gemini_flavor.generate_flavor_text(gemini_api_key, summary, gemini_model)
    if flavor:
        embed["description"] = flavor

    _post(webhook_url, embed)
