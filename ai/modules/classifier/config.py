SUSPICIOUS_OBJECTS = {"handbag", "bag", "box", "envelope", "wallet", "package", "bottle"}

DANGEROUS_OBJECTS = {"gun", "knife", "fire", "smoke", "pistol", "rifle", "scissor", "blade"}


CROWD_PERSON_COUNT = 5

# OCR keywords
POLITICAL_KEYWORDS = {
    "vote", "voting", "candidate", "party", "election", "support", "vote for",
    "votefor", "vote_for", "मत", "छापा", "समर्थन", "उपहार", "निःशुल्क", "सहयोग", "free", "masubhat", "petrol"
}

# Bribery-related keywords
BRIBERY_KEYWORDS = {"gift", "donation", "free", "support", "help", "reward",
                    "उपहार", "दान", "निःशुल्क", "सहयोग", "समर्थन"}

# scoring weights (percent-based)
WEIGHTS = {
    "crowd": 30,            # crowd presence
    "suspicious_object": 25,# object that looks like cash/envelope/bag
    "party_text": 20,       # party name / political keywords in OCR
    "bribery_text": 25,     # explicit bribery words in OCR
    "repeated_reports": 10  # (optional) multiple reports nearby - integrate later
}

# max risk cap
MAX_RISK = 100
