from fastapi import FastAPI, UploadFile, File
from modules.detector.load import load_models
from modules.detector.detect import detect_image
from modules.classifier.classify import classify
from modules.utils.video import process_video

app = FastAPI()

flag_model, person_model = load_models()

@app.post("/analyze")
async def analyze(file: UploadFile = File(...)):
    file_path = f"temp_{file.filename}"
    with open(file_path, "wb") as f:
        f.write(await file.read())
    
    if file.filename.endswith(".mp4"):
        results = process_video(file_path, flag_model, person_model, classify)
    else:
        detect_results = detect_image(file_path, flag_model, person_model)
        results = classify(detect_results)
    
    return results

