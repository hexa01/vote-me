import os
import shutil
import re
import numpy as np
from fastapi.encoders import jsonable_encoder
from fastapi import FastAPI, UploadFile, File
from modules.detector.load import load_models
from modules.detector.detect import detect_image
from modules.classifier.classify import classify
from modules.utils.video import process_video
from modules.utils.ocr import ocr_image
from modules.manifesto.extract import extract_text_from_pdf
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity

app = FastAPI()

# Load AI models
flag_model, person_model = load_models()

import numpy as np

def convert_to_serializable(obj):
    """
    Recursively convert NumPy / torch types to Python native types.
    Works for dict, list, tuple, int, float, str.
    """
    import torch

    if isinstance(obj, dict):
        return {k: convert_to_serializable(v) for k, v in obj.items()}
    elif isinstance(obj, list):
        return [convert_to_serializable(v) for v in obj]
    elif isinstance(obj, tuple):
        return tuple(convert_to_serializable(v) for v in obj)
    elif isinstance(obj, (np.integer,)):
        return int(obj)
    elif isinstance(obj, (np.floating,)):
        return float(obj)
    elif isinstance(obj, (np.ndarray, torch.Tensor)):
        return convert_to_serializable(obj.tolist())
    else:
        return obj

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
            "ai_tags": classified_results.get('ai_tags'),
            "risk_score": classified_results.get('risk_score'),
            "summary": classified_results.get('summary'),
            "objects": detect_results,
            "ocr": ocr_results
        }

    try:
        os.remove(tmp_file_path)
    except:
        pass
    
    # Convert all nested objects to Python native types
    response_serializable = convert_to_serializable(response)
    return response_serializable

def split_paragraphs(text):
    return [p.strip() for p in text.split("\n") if len(p.strip()) > 20]

def simple_extractive_summary(text, max_sentences=5):
    sentences = re.split(r'(?<=[.!?])\s+', text.strip())
    scored = sorted(sentences, key=lambda s: len(s), reverse=True)
    return " ".join(scored[:max_sentences])

def compare_texts_simple(text_a, text_b, top_k=5):
    paras_a = split_paragraphs(text_a)
    paras_b = split_paragraphs(text_b)
    all_paras = paras_a + paras_b

    vectorizer = TfidfVectorizer().fit(all_paras)
    tfidf_a = vectorizer.transform(paras_a)
    tfidf_b = vectorizer.transform(paras_b)

    cos_scores = cosine_similarity(tfidf_a, tfidf_b)
    overall_score = float(cos_scores.mean())

    # Top similar paragraph pairs
    flat = cos_scores.flatten()
    idxs = np.argsort(-flat)[:top_k]
    top_pairs = []
    for idx in idxs:
        i = int(idx // cos_scores.shape[1])
        j = int(idx % cos_scores.shape[1])
        score = float(cos_scores[i, j])
        top_pairs.append({
            "score": round(score, 3),
            "para_a": paras_a[i],
            "para_b": paras_b[j]
        })

    # Unique paragraphs
    unique_a = [p for i,p in enumerate(paras_a) if not any(cos_scores[i,j]>0.5 for j in range(len(paras_b)))]
    unique_b = [p for j,p in enumerate(paras_b) if not any(cos_scores[i,j]>0.5 for i in range(len(paras_a)))]

    return {
        "overall_score": round(overall_score,3),
        "top_pairs": top_pairs,
        "summary_a": simple_extractive_summary(text_a),
        "summary_b": simple_extractive_summary(text_b),
        "unique_a": unique_a[:5],
        "unique_b": unique_b[:5]
    }


@app.post("/manifesto/compare_summary")
async def manifesto_compare_summary(file_a: UploadFile = File(...), file_b: UploadFile = File(...)):
    tmp_a = f"tmp_{file_a.filename}"
    tmp_b = f"tmp_{file_b.filename}"

    # Save uploaded files
    with open(tmp_a, "wb") as f:
        shutil.copyfileobj(file_a.file, f)
    with open(tmp_b, "wb") as f:
        shutil.copyfileobj(file_b.file, f)

    # Extract text from PDFs (with OCR fallback)
    text_a = extract_text_from_pdf(tmp_a)
    text_b = extract_text_from_pdf(tmp_b)

    # Compare texts
    result = compare_texts_simple(text_a, text_b)

    # Cleanup
    try:
        os.remove(tmp_a)
        os.remove(tmp_b)
    except Exception:
        pass

    return result
