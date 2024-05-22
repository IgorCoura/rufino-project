import pdfplumber
import os
from unidecode import unidecode
import csv

def formatar_string(s):
    # Remove espaços
    s = s.replace(" ", "")
    # Converte para maiúsculas
    s = s.upper()
    # Substitui letras com acento
    s = unidecode(s)
    return s

def get_lines_csv(path):
    list = []
    with open(path, 'r', encoding='utf-8') as arquivo_csv:
        leitor_csv = csv.reader(arquivo_csv)
        for linha in leitor_csv:
            list.append(linha)
    return list

def get_text_pdf(base_path, name):
    pdf_path = base_path + "/" + name
    pdf = pdfplumber.open(pdf_path)
    text = ""
    for page in pdf.pages:
        list_text = page.extract_text()
        for text_line in list_text:
            text += text_line
    pdf.close()
    return formatar_string(text)

def text_contains_name_in_list(text, list_names):
    for line in list_names:
        name = line[0]
        name_normalize = formatar_string(line[0])
        if name_normalize in text:
            return name
    return None

def rename_file(base_path, current_name, new_name):
    path_current_name = base_path +"/" + current_name
    path_new_name = base_path + "/" + new_name
    os.rename(path_current_name, path_new_name)
    
def put_name_files(base_pdf_path, name_folder, name_pdf, csv_path):
    text_pdf = get_text_pdf(base_pdf_path, name_pdf)
    list_of_lines = get_lines_csv(csv_path)
    match_name = text_contains_name_in_list(text_pdf, list_of_lines)
    if match_name == None:
        return
    new_name = name_folder + " - " + match_name + ".pdf"
    if match_name != None:
        rename_file(base_pdf_path, name_pdf, new_name)



base_path = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + "/NomeacaoDePDFs"

csv_path = base_path + '/data/funcionarios.csv'
folders_base = base_path + '/input'

folders = os.listdir(folders_base)

for folder in folders:
    files_base = folders_base +"/" +folder
    files = os.listdir(files_base)
    for file in files:
        put_name_files(files_base, folder, file, csv_path)






