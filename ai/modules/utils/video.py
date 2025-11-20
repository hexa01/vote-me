import cv2
from modules.detector.detect import detect_image
from modules.classifier.classify import classify

def process_video(video_path, flag_model, person_model, classifier):
    cap = cv2.VideoCapture(video_path)
    frame_results = []
    frame_interval = 30  # analyze 1 frame every 30 frames (~1 sec)

    count = 0
    while True:
        ret, frame = cap.read()
        if not ret:
            break
        if count % frame_interval == 0:
            cv2.imwrite("temp_frame.jpg", frame)
            detect_results = detect_image("temp_frame.jpg", flag_model, person_model)
            classified = classifier(detect_results)
            frame_results.append(classified)
        count += 1
    cap.release()

    # Aggregate results for whole video
    final_tags = set()
    max_risk = 0
    for fr in frame_results:
        final_tags.update(fr['ai_tags'])
        max_risk = max(max_risk, fr['risk_score'])
    
    summary = " | ".join(list(final_tags))
    return {
        "ai_tags": list(final_tags),
        "risk_score": max_risk,
        "summary": summary
    }
