def classify(detect_results):
    tags = []
    if len(detect_results['people']) > 3:
        tags.append("crowd_present")
    if len(detect_results['flags']) > 0:
        tags.append("flag_present")
    
    # Simple risk score
    risk_score = len(tags) / 2  # 0, 0.5, 1.0
    summary = "Crowd detected, flag present." if len(tags) == 2 else ", ".join(tags)
    
    return {
        "ai_tags": tags,
        "risk_score": risk_score,
        "summary": summary
    }
