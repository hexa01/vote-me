# extract.py
import os
import shutil
from PyPDF2 import PdfReader
from PIL import Image
import pytesseract
from pdf2image import convert_from_path
from fastapi import FastAPI, UploadFile, File
from sklearn.feature_extraction.text import CountVectorizer
from sklearn.metrics.pairwise import cosine_similarity

app = FastAPI()

def extract_text_from_pdf(pdf_path, try_ocr=True):
    text_chunks = []
    try:
        reader = PdfReader(pdf_path)
        for page in reader.pages:
            text_chunks.append(page.extract_text() or "")
    except Exception:
        text_chunks = [""]

    full_text = "\n".join(text_chunks).strip()

    # If text is very short, try OCR
    if len(full_text) < 200 and try_ocr:
        try:
            pages = convert_from_path(pdf_path, dpi=200)
            ocr_texts = [pytesseract.image_to_string(p, lang='eng+nep') for p in pages]
            ocr_full = "\n".join(ocr_texts).strip()
            if ocr_full:
                full_text = ocr_full
        except Exception:
            try:
                im = Image.open(pdf_path)
                ocr_text = pytesseract.image_to_string(im, lang='eng+nep')
                if ocr_text.strip():
                    full_text = ocr_text
            except Exception:
                pass

    return full_text

def compare_texts_simple(text_a: str, text_b: str):
    # Clean and normalize
    text_a = " ".join(text_a.split())
    text_b = " ".join(text_b.split())

    if not text_a or not text_b:
        return {"error": "One or both PDFs are empty or unreadable."}

    vectorizer = CountVectorizer(
        stop_words=None,
        analyzer=lambda x: x.split()  # works for Nepali + English
    )
    vectors = vectorizer.fit_transform([text_a, text_b])
    similarity = cosine_similarity(vectors[0], vectors[1])[0][0]

    return {
        "similarity_score": round(float(similarity), 4),
        "text_length_a": len(text_a),
        "text_length_b": len(text_b)
    }


