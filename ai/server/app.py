import os
from fastapi import FastAPI, UploadFile, File
from modules.detector.load import load_models
from modules.detector.detect import detect_image
from modules.classifier.classify import classify
from modules.utils.video import process_video
from modules.utils.ocr import ocr_image

app = FastAPI()

flag_model, person_model = load_models()

@app.post("/analyze")
async def analyze(file: UploadFile = File(...)):
    tmp_file_path = f"temp_{file.filename}"
    with open(tmp_file_path, "wb") as f:
        f.write(await file.read())
    
    if file.filename.endswith(".mp4"):
        response = process_video(tmp_file_path, flag_model, person_model, classify)
    else:
        detect_results = detect_image(tmp_file_path, flag_model, person_model)
        ocr_results = ocr_image(tmp_file_path)
        classified_results = classify(detect_results)

        response = {
            "ai_tags":classified_results.get('ai_tags'),
            "risk_score":classified_results.get('risk_score'),
            "summary": classified_results.get('summary'),
            "objects": detect_results,
            "ocr": ocr_results
        }

    try:
        os.remove(tmp_file_path)
    except:
        pass
    
    return response

