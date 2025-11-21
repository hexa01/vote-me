from typing import Dict, Any, List
from . import config

def _normalize_detect(detect_results: Dict[str, Any]) -> Dict[str, Any]:
    """
    Convert various detector output shapes into a standard dict:
    {
      "objects": [ {"name": "person", "confidence": 0.9}, ... ],
      "counts": {"person": 5, "handbag": 1, ...}
    }
    """
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
    """
    Input:
      - detect_results: dict returned by detect_image(...)
      - ocr_text: extracted text (string) for that frame/image
    Output:
      - dict with keys: ai_tags (list), risk_score (0-100 int), summary (string), reasons (list)
    """
    norm = _normalize_detect(detect_results)
    counts = norm["counts"]
    objects = norm["objects"]
    tags = []
    reasons = []
    score = 0.0

    person_count = counts.get("person", 0)

    # crowd presence
    if person_count >= config.CROWD_PERSON_COUNT:
        tags.append("crowd_present")
        reasons.append(f"Large crowd detected ({person_count} people)")
        score += config.WEIGHTS.get("crowd", 0)

    # suspicious object
    suspicious_found = False
    for obj_name in counts.keys():
        if obj_name in config.SUSPICIOUS_OBJECTS:
            suspicious_found = True
            tags.append("suspicious_object_present")
            reasons.append(f"Detected suspicious object: {obj_name} ({counts[obj_name]})")
            score += config.WEIGHTS.get("suspicious_object", 0)
            break

    # political text present in OCR
    political_hits = _contains_keyword(ocr_text, config.POLITICAL_KEYWORDS)
    if political_hits:
        tags.append("political_text_present")
        reasons.append(f"Political keywords found: {', '.join(political_hits[:5])}")
        score += config.WEIGHTS.get("party_text", 0)

    # bribery-related text
    bribery_hits = _contains_keyword(ocr_text, config.BRIBERY_KEYWORDS)
    if bribery_hits:
        tags.append("bribery_text_present")
        reasons.append(f"Bribery keywords found: {', '.join(bribery_hits[:5])}")
        score += config.WEIGHTS.get("bribery_text", 0)

    # escalate if crowd and sus objets
    if person_count >= config.CROWD_PERSON_COUNT and suspicious_found:
        tags.append("crowd_with_object")
        reasons.append("Crowd present while suspicious object detected (possible exchange)")
        score += 10 #boost

    # normalization
    score = min(score, config.MAX_RISK)
    score = int(round(score))

    # tags create
    if tags:
        summary = "; ".join(tags)
    else:
        summary = "no_suspicious_activity_detected"

    return {
        "ai_tags": tags,
        "risk_scoore": score,
        "summary": summary,
        "reasons": reasons,
        "counts": counts,
        "ocr_text": ocr_text or ""
    }
