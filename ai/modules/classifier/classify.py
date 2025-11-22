from typing import Dict, Any, List
from . import config

def _normalize_detect(detect_results: Dict[str, Any]) -> Dict[str, Any]:
    out = {"objects": [], "counts": {}}
    if not detect_results:
        return out

    if isinstance(detect_results, dict) and "objects" in detect_results:
        objs = detect_results["objects"]
    else:
        objs = []

        if "people" in detect_results and isinstance(detect_results["people"], list):
            for p in detect_results["people"]:
                objs.append({"name": "person", "confidence": float(p.get("confidence", 0.0))})
        # flags
        if "flags" in detect_results and isinstance(detect_results["flags"], list):
            for f in detect_results["flags"]:

                name = f.get("class", "flag") if isinstance(f, dict) else "flag"
                objs.append({"name": "flag", "confidence": float(f.get("confidence", 0.0)) if isinstance(f, dict) else 0.0})

        if "raw" in detect_results:

            try:
                for r in detect_results["raw"].get("results", []):
                    for box in r.get("boxes", []):
                        cls = box.get("class") or box.get("cls") or box.get("name")
                        name = str(cls)
                        objs.append({"name": name, "confidence": float(box.get("confidence", 0.0))})
            except Exception:
                pass

    # Build counts
    counts = {}
    for o in objs:
        name = str(o.get("name", "")).lower()
        counts[name] = counts.get(name, 0) + 1

    out["objects"] = objs
    out["counts"] = counts
    return out


def _contains_keyword(text: str, keywords: set) -> List[str]:
    found = []
    if not text:
        return found
    t = text.lower()
    for kw in keywords:
        if kw in t:
            found.append(kw)
    return found

def classify(detect_results: Dict[str, Any], ocr_text: str = "") -> Dict[str, Any]:
    norm = _normalize_detect(detect_results)
    counts = norm["counts"]
    objects = norm["objects"]
    tags = []
    reasons = []
    score = 0.0

    person_count = counts.get("person", 0)

    # Initial flags
    dangerous_found = False
    dangerous_objects_detected = []
    suspicious_found = False

    # DETECT DANGEROUS OBJECTS
    for obj_name in counts.keys():
        # Guns, knives, fire, smoke etc.
        if obj_name in config.DANGEROUS_OBJECTS:
            dangerous_found = True
            dangerous_objects_detected.append(obj_name)
            tags.append("dangerous_object_present")
            reasons.append(f"Dangerous object detected: {obj_name} ({counts[obj_name]})")
            score += 20  # heavy weight

        # Suspicious objects (your older list)
        if obj_name in config.SUSPICIOUS_OBJECTS:
            suspicious_found = True
            tags.append("suspicious_object_present")
            reasons.append(f"Suspicious object detected: {obj_name} ({counts[obj_name]})")
            score += config.WEIGHTS.get("suspicious_object", 0)

    # CROWD DETECTION
    if person_count >= config.CROWD_PERSON_COUNT:
        tags.append("crowd_present")
        reasons.append(f"Large crowd detected ({person_count} people)")
        score += config.WEIGHTS.get("crowd", 0)

    # 1 PERSON + WEAPON/FIRE = HIGH RISK
    if person_count >= 1 and dangerous_found:
        tags.append("person_with_weapon_or_fire")
        reasons.append(
            f"{person_count} person(s) present with dangerous items: {', '.join(dangerous_objects_detected)}"
        )
        score += 25

    # CROWD + DANGEROUS OBJECT
    if person_count >= config.CROWD_PERSON_COUNT and (dangerous_found or suspicious_found):
        tags.append("crowd_with_danger")
        reasons.append("Crowd present with dangerous or suspicious items")
        score += 10

    # POLITICAL TEXT
    political_hits = _contains_keyword(ocr_text, config.POLITICAL_KEYWORDS)
    if political_hits:
        tags.append("political_text_present")
        reasons.append(f"Political keywords found: {', '.join(political_hits[:5])}")
        score += config.WEIGHTS.get("party_text", 0)

    # BRIBERY TEXT
    bribery_hits = _contains_keyword(ocr_text, config.BRIBERY_KEYWORDS)
    if bribery_hits:
        tags.append("bribery_text_present")
        reasons.append(f"Bribery keywords found: {', '.join(bribery_hits[:5])}")
        score += config.WEIGHTS.get("bribery_text", 0)

    # FINAL SCORING
    score = int(min(score, config.MAX_RISK))
    normalized_score = score / config.MAX_RISK

    summary = "; ".join(tags) if tags else "no_suspicious_activity_detected"

    return {
        "ai_tags": tags,
        "risk_score": normalized_score,
        "summary": summary,
        "reasons": reasons,
        "counts": counts,
        "ocr_text": ocr_text or ""
    }
