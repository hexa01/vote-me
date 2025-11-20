import cv2

def detect_image(image_path, flag_model, person_model):
    results = {}

    # detect any object
    flag_preds = flag_model(image_path)
    results['flags'] = []
    for r in flag_preds:
        for box in r.boxes:
            results['flags'].append({
                "class": int(box.cls),
                "confidence": float(box.conf[0]),
                "xyxy": box.xyxy.tolist()
            })

    # detect person
    person_preds = person_model(image_path)
    results['people'] = []
    for r in person_preds:
        for box in r.boxes:
            results['people'].append({
                "confidence": float(box.conf[0]),
                "xyxy": box.xyxy.tolist()
            })

    return results
