def classify(detect_results, ocr_text = ""):
    tags = []
    if len(detect_results['people']) > 3:
        tags.append("crowd_present")
    if len(detect_results['flags']) > 0:
        tags.append("flag_present")

    
    # OCR-based rule
    ocr_text_lower = (ocr_text or "").lower()
    political_words = ["vote", "party", "candidate", "छापा", "मत"]
    if any(w in ocr_text_lower for w in political_words):
        tags.append("political_text_present")

    # Simple risk score
    base = len(tags) / 3.0
    risk_score = min(base, 1.0)
    summary = ", ".join(tags) if tags else "no_suspicious_activity_detected"
    
    return {
        "ai_tags": tags,
        "risk_score": risk_score,
        "summary": summary
    }
