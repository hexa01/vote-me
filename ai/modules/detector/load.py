from ultralytics import YOLO

def load_models():
    #later when we train model and put it in models/
    # flag_model = YOLO("models/yolo_flags.pt")
    # person_model = YOLO("models/yolo_person.pt")  

    # temporary workaround for mvp
    person_model = YOLO("yolov8n.pt")  
    flag_model = YOLO("yolov8n.pt")  
    return flag_model, person_model
