import cv2

def detect_image(image_path, flag_model, person_model):
    results = {
        "flags": [],
        "people": []
    }

    # flag/symbol
    try:
        flag_preds = flag_model(image_path)

        for pred in flag_preds:
            for box in pred.boxes:
                results["flags"].append({
                    "class": int(box.cls),
                    "confidence": float(box.conf[0]),
                    "xyxy": box.xyxy[0].tolist()
                })

    except Exception as e:
        print("Flag detection error:", e)

    # person
    try:
        person_preds = person_model(image_path)

        for pred in person_preds:
            for box in pred.boxes:
                results["people"].append({
                    "confidence": float(box.conf[0]),
                    "xyxy": box.xyxy[0].tolist()
                })

    except Exception as e:
        print("Person detection error:", e)

    return results
