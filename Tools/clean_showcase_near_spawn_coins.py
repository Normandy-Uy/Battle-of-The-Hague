#!/usr/bin/env python3
"""Remove DutzGoldCoin_NearSpawn_* prefab instances from Dutz_Showcase.unity."""

import re
from pathlib import Path

SCENE = Path(__file__).resolve().parents[1] / "Assets" / "Scenes" / "Dutz_Showcase.unity"

COIN_PREFIX = "DutzGoldCoin_NearSpawn_"


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
        if name.startswith(COIN_PREFIX):
            removable.add(doc["id"])
    return removable


def doc_references_id(text: str, ids: set[int]) -> bool:
    for pid in ids:
        if re.search(rf"fileID: {pid}\b", text):
            return True
    return False


def clean_transform_children(text: str, removed_transform_ids: set[int]) -> str:
    lines = text.splitlines(keepends=True)
    out = []
    in_children = False
    for line in lines:
        if re.match(r"^  m_Children:\s*$", line):
            in_children = True
            out.append(line)
            continue
        if in_children:
            child_match = re.match(r"^  - \{fileID: (\d+)\}\s*$", line)
            if child_match:
                if int(child_match.group(1)) not in removed_transform_ids:
                    out.append(line)
                continue
            in_children = False
        out.append(line)
    return "".join(out)


def main():
    text = SCENE.read_text(encoding="utf-8")
    docs = parse_docs(text)
    removable_prefab_ids = find_removable_prefab_ids(docs)

    removed_transform_ids = set()
    for doc in docs:
        if doc["type"] != 4:
            continue
        if not doc_references_id(doc["text"], removable_prefab_ids):
            continue
        removed_transform_ids.add(doc["id"])

    filtered = []
    for doc in docs:
        if doc["id"] is not None and doc["id"] in removable_prefab_ids:
            continue
        if doc["text"] and doc_references_id(doc["text"], removable_prefab_ids):
            continue
        filtered.append(doc["text"])

    result = "".join(filtered)
    if removed_transform_ids:
        result = clean_transform_children(result, removed_transform_ids)

    SCENE.write_text(result, encoding="utf-8")
    print(f"Removed {len(removable_prefab_ids)} near-spawn coins from {SCENE.name}")


if __name__ == "__main__":
    main()
