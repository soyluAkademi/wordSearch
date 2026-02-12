import json
import os

# Source questions with correct Turkish characters
questions_source = [
    {"question": "Osmanlı padişahlarının özel imzasına verilen ad", "answer": "TUĞRA"},
    {"question": "Eski Türklerde hükümdarlara verilen genel ad", "answer": "HAKAN"},
    {"question": "Tarihin ilk yazılı antlaşmasına verilen ad", "answer": "KADEŞ"},
    {"question": "Antik Mısır hükümdarlarına verilen unvan", "answer": "FİRAVUN"},
    {"question": "Antik Yunan şehir devletlerine verilen ad", "answer": "POLİS"},
    {"question": "Osmanlı Devleti'nin kurucusu olan beyin adı", "answer": "OSMAN"},
    {"question": "Eski Türklerde meclis/şölen anlamına gelen kelime", "answer": "TOY"},
    {"question": "Selçuklu ve Osmanlı'daki anıt mezarlara verilen ad", "answer": "KÜMBET"},
    {"question": "Tarihte parayı ilk kez kullanan uygarlığa verilen ad", "answer": "LİDYA"},
    {"question": "Osmanlı'da üst düzey devlet adamı yetiştiren okul", "answer": "ENDERUN"}
]

all_questions = []
id_counter = 1

# Generate 1500 questions (150 loops of 10)
for _ in range(150):
    for q in questions_source:
        new_q = q.copy()
        new_q['id'] = id_counter
        all_questions.append(new_q)
        id_counter += 1

data = {"questions": all_questions}

# Write to file with UTF-8 encoding and ensure_ascii=False to keep Turkish chars
target_path = r"c:\Users\ssoylu\Desktop\yeniGithub\wordSearch\Assets\Resources\questions.json"
with open(target_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=2)

print(f"Successfully generated {id_counter-1} questions in {target_path}")
