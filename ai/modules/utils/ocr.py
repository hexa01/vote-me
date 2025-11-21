# modules/utils/ocr.py
import easyocr
import os
from PIL import Image

#avoid reload model again again single reader
_reader = None

def get_reader(device='cpu', lang_list=None):
    global _reader
    if _reader is None:
        # ne nepali en english
        if lang_list is None:
            lang_list = ['ne', 'en']
        _reader = easyocr.Reader(lang_list, gpu=False)
    return _reader

def ocr_image(image_path, lang_list=None, rotate=False):
    """
    Run EasyOCR on an image and return concatenated text and raw results.
    rotate: if True, the function will try simple 90-degree rotations if no text found
    """
    reader = get_reader(lang_list=lang_list)
    img = Image.open(image_path).convert('RGB')
    img_np = __pil_to_np(img)

    results = reader.readtext(img_np)
    if not results and rotate:
        img_rot = img.rotate(90, expand=True)
        results = reader.readtext(__pil_to_np(img_rot))

    texts = [res[1] for res in results if len(res) > 1]
    full_text = " ".join(texts).strip()
    return {
        "text": full_text,
        "segments": [
            {"bbox": res[0], "text": res[1], "confidence": float(res[2])}
            for res in results
        ]
    }

def __pil_to_np(pil_img):
    """Convert PIL image to numpy array (RGB) for easyocr"""
    import numpy as np
    return np.array(pil_img)
