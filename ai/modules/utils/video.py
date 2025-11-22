import cv2
from modules.detector.detect import detect_image
from modules.classifier.classify import classify
from modules.utils.ocr import ocr_image
import os
def process_video(video_path, flag_model, person_model, classifier):
    cap = cv2.VideoCapture(video_path)

    frame_interval = 30  # 1 frame per sec
    count = 0

    classification_all = []
    ocr_all = []
    frame_results = []  # for summary

    while True:
        ret, frame = cap.read()
        if not ret:
            break

        if count % frame_interval == 0:
            temp_img = "temp_frame.jpg"
            cv2.imwrite(temp_img, frame)

            detect_results = detect_image(temp_img, flag_model, person_model)

            frame_results.append(detect_results)

            try:
                ocr_res = ocr_image(temp_img)
            except Exception:
                ocr_res = {"text": "", "segments": []}

            ocr_all.append(ocr_res)

            cls_res = classify(detect_results, ocr_res.get("text", ""))
            cls_res["risk_score"] = min(cls_res.get("risk_score", 0) / 100, 1.0)
            classification_all.append(cls_res)

        count += 1

    cap.release()

    # risk/confidence scoe
    if len(classification_all) > 0:
        avg_score = sum([c["risk_score"] for c in classification_all]) / len(classification_all)
    else:
        avg_score = 0.0

    # -tags
    tags = set()
    for c in classification_all:
        for t in c.get("ai_tags", []):
            tags.add(t)

    # combine
    ocr_combined = " ".join([o.get("text", "") for o in ocr_all if o.get("text")])

    # detection  test for test
    detections_lite = frame_results[:10]

    summary_text = " | ".join(list(tags))

    tag_counts = {}
    for c in classification_all:
        for t in c.get("ai_tags", []):
            tag_counts[t] = tag_counts.get(t, 0) + 1

    reasons = [f"Detected '{t}' in multiple frames" for t in tags]

    return {
        "risk_score": round(avg_score, 2),
        "ai_tags": list(tags),
        "summary": summary_text,
        "reasons": reasons,
        "tag_counts": tag_counts,
        "ocr_text": ocr_combined,
        "detections": detections_lite,
        "frames_analyzed": len(classification_all),
    }

