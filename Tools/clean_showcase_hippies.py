#!/usr/bin/env python3
"""Remove baked small hippies from Dutz_Showcase.unity; keep DutzSegmentHippie pool + giants."""

import re
import sys
from pathlib import Path

SCENE = Path(__file__).resolve().parents[1] / "Assets" / "Scenes" / "Dutz_Showcase.unity"

REMOVE_PREFIXES = (
    "SimpleCitizens_Hippie_Black",
    "SimpleCitizens_Hippie_Extra_",
    "SimpleCitizens_Hippie_NearSpawn_",
    "SimpleCitizens_Hippie_Flying_",
)

KEEP_NAMES = {
    "SimpleCitizens_Hippie_Giant",
    "SimpleCitizens_Hippie_Giant_Mid",
    "SimpleCitizens_Grandma_White",
}


def should_remove_name(name: str) -> bool:
    if not name:
        return False
    if name in KEEP_NAMES:
        return False
    if name.startswith("DutzSegmentHippie_"):
        return False
    return any(name.startswith(p) for p in REMOVE_PREFIXES)


def parse_docs(text: str):
    parts = re.split(r"(?=--- !u!)", text)
    docs = []
    for part in parts:
        if not part.strip():
            continue
        header = re.match(r"--- !u!(\d+) &(\d+)", part)
        if not header:
            docs.append({"text": part, "type": None, "id": None})
            continue
        docs.append({"text": part, "type": int(header.group(1)), "id": int(header.group(2))})
    return docs


def find_removable_prefab_ids(docs):
    removable = set()
    for doc in docs:
        if doc["type"] != 1001:
            continue
        name_match = re.search(
            r"propertyPath: m_Name\s*\n\s*value: ([^\n]+)", doc["text"]
        )
        if not name_match:
            continue
        name = name_match.group(1).strip()
        if should_remove_name(name):
            removable.add(doc["id"])
    return removable


def doc_references_id(text: str, ids: set[int]) -> bool:
    for pid in ids:
        if re.search(rf"fileID: {pid}\b", text):
            return True
    return False


def clean_scene_roots(text: str, removed_ids: set[int]) -> str:
    def repl_line(match):
        fid = int(match.group(1))
        return "" if fid in removed_ids else match.group(0)

    return re.sub(r"^  - \{fileID: (\d+)\}\s*$", repl_line, text, flags=re.MULTILINE)


def main():
    text = SCENE.read_text(encoding="utf-8")
    docs = parse_docs(text)
    removable_prefab_ids = find_removable_prefab_ids(docs)

    removed_doc_ids = set(removable_prefab_ids)
    filtered = []
    for doc in docs:
        if doc["id"] is not None and doc["id"] in removed_doc_ids:
            continue
        if doc["text"] and doc_references_id(doc["text"], removable_prefab_ids):
            continue
        filtered.append(doc["text"])

    result = "".join(filtered)
    result = clean_scene_roots(result, removable_prefab_ids)
    SCENE.write_text(result, encoding="utf-8")
    print(f"Removed {len(removable_prefab_ids)} baked hippie prefab instances from {SCENE.name}")


if __name__ == "__main__":
    main()
