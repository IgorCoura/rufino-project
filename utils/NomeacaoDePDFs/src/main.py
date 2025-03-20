import os
from unidecode import unidecode
import csv
import pytesseract
from PIL import Image
import sys, pathlib, pymupdf

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
        leitor_csv = csv.reader(arquivo_csv, delimiter=";")
        for linha in leitor_csv:
            list.append(linha)
    return list

def get_text_image(page):
    nameImage = "page-%i.png" % page.number
    page.get_pixmap().save(nameImage) 
    pytesseract.pytesseract.tesseract_cmd = r'C:\Program Files\Tesseract-OCR\tesseract.exe'
    text = pytesseract.image_to_string(Image.open(nameImage))
    os.remove(nameImage)
    return text

def get_text_pdf(base_path, name):
    pdf_path = base_path + "/" + name
    text = ""
    with pymupdf.open(pdf_path) as doc:
        for page in doc:
            text += page.get_text()        
            text += get_text_image(page)
    return formatar_string(text)

def text_contains_name_in_list(text, list_names):
    max = 3
    for i in range(max):
        name = check_name_in_list(text, list_names, i)
        return name
    return None

def check_name_in_list(text, list_names, remove_chars):
    for line in list_names:
        first_name = line[0]
        for name in line:
            name_normalize = formatar_string(name)
            if(remove_chars > 0):
                name_normalize = name_normalize[remove_chars:-remove_chars]
            if name_normalize == None or name_normalize == '':
                continue
            if name_normalize in text:
                return first_name
    return None

def rename_file(base_path, current_name, new_name):
    path_current_name = base_path +"/" + current_name
    path_new_name = base_path + "/" + unidecode(new_name, "utf-8")
    os.rename(path_current_name, path_new_name)
    
def put_name_files(base_pdf_path, name_folder, name_pdf, list_of_lines):
    text_pdf = get_text_pdf(base_pdf_path, name_pdf)
    match_name = text_contains_name_in_list(text_pdf, list_of_lines)
    if match_name == None:
        return
    new_name = name_folder + " - " + match_name + ".pdf"
    rename_file(base_pdf_path, name_pdf, new_name)
    return match_name

def find_name_in_list(name, list):
    name_normalize = formatar_string(name)
    for item in list:
        item_normalize = formatar_string(item)
        if name_normalize == item_normalize:
            return True
    return False

def check_name_list_not_used(folder_name, files_renames, list_name, list_names_not_found):
    for name in list_name:
        if find_name_in_list(name[0], files_renames) == False:
            return_name = unidecode(name[0], "utf-8")
            list_names_not_found.append(f"Não encontrado: {folder_name} - {return_name}")

print("Incio")
base_path = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + "/NomeacaoDePDFs"

csv_path = base_path + '/data/funcionarios.csv'
folders_base = base_path + '/input'

folders = os.listdir(folders_base)
list_of_lines = get_lines_csv(csv_path)

list_errors = []
list_names_not_found = []

for folder in folders:
    files_base = folders_base +"/" +folder
    files = os.listdir(files_base)
    files_renames = []
    count = len(files)
    for file in files:
        try:
            name = put_name_files(files_base, folder, file, list_of_lines)
            if name == None:
                continue
            files_renames.append(name)
        except Exception as e:
            list_errors.append(f"{e}")
            continue
            count -= 1
            print(f"{folder}: {count}/{len(files)}")
    check_name_list_not_used(folder, files_renames, list_of_lines, list_names_not_found)
        
print("Errors Lançados")
for erro in list_errors:
    print(erro)
    
print("Nomes não encontrados")
for name in list_names_not_found:
    print(name)
    
print("Fim")




