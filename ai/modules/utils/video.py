import cv2
from modules.detector.detect import detect_image
from modules.classifier.classify import classify
from modules.utils.ocr import ocr_image
import os

def process_video(video_path, flag_model, person_model, classifier):
    cap = cv2.VideoCapture(video_path)
    frame_results = []
    frame_interval = 30  # analyze 1 frame every 30 frames (~1 sec)
    count = 0

    ocr_total = []
    classification_final = []

    while True:
        ret, frame = cap.read()
        if not ret:
            break

        if count % frame_interval == 0:
            cv2.imwrite("temp_frame.jpg", frame)

            detect_results = detect_image("temp_frame.jpg", flag_model, person_model)

            classified = classifier(detect_results)
            frame_results.append(classified)

            try:
                ocr_res = ocr_image("temp_frame.jpg")
            except Exception:
                ocr_res = {"text": "", "segments": []}
            ocr_total.append(ocr_res)

            cls_res = classify(detect_results, ocr_res.get("text", ""))
            classification_final.append(cls_res)

        count += 1

    cap.release()

    if len(classification_final) > 0:
        avg_score = sum([c['risk_score'] for c in classification_final]) / len(classification_final)
    else:
        avg_score = 0.0

    tags = set()
    for c in classification_final:
        for t in c.get("ai_tags", []):
            tags.add(t)

    ocr_combined = " ".join([o.get("text", "") for o in ocr_total if o.get("text")])

    detections_lite = frame_results[:10]

    return {
        "risk_score": round(avg_score, 2),
        "tags": list(tags),
        "frames_analyzed": len(classification_final),
        "ocr_combined_text": ocr_combined,
        "detections": detections_lite,
    }
