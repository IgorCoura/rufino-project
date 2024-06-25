import PyPDF2
import os
from unidecode import unidecode

def merge_pdfs(pdf1_path, pdf2_path, output_path):
    pdf_writer = PyPDF2.PdfWriter()

    # Abrir o primeiro arquivo PDF
    with open(pdf1_path, 'rb') as pdf1_file:
        pdf1_reader = PyPDF2.PdfReader(pdf1_file)
        for page_num in range(len(pdf1_reader.pages)):
            page = pdf1_reader.pages[page_num]
            pdf_writer.add_page(page)

    # Abrir o segundo arquivo PDF
    with open(pdf2_path, 'rb') as pdf2_file:
        pdf2_reader = PyPDF2.PdfReader(pdf2_file)
        for page_num in range(len(pdf2_reader.pages)):
            page = pdf2_reader.pages[page_num]
            pdf_writer.add_page(page)

    # Escrever o conteúdo combinado em um novo arquivo PDF
    with open(output_path, 'wb') as merged_pdf_file:
        pdf_writer.write(merged_pdf_file)

    print(f'Os PDFs foram combinados com sucesso em: {output_path}')
    
def find_combine(file, list_file):
    return ""

def normalize_list(list_file):
    list = []
    for file in list_file:
        list.append(normalize_name(file))
    return list

def normalize_name(name):
    name = name[5:]
    name = name.replace(" ", "")
    name = name.replace(".pdf", "")
    name = name.upper()
    name = unidecode(name)
    return name


print("Incio")
base_path = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))) + "/combinePDFs"

folders_input = base_path + '/input'
folders_output = base_path + '/output'

folder_merge1 = folders_input + '/MARGE1'
folder_merge2 = folders_input + '/MARGE2'

list_file_merge1 = os.listdir(folder_merge1)
list_file_merge2 = os.listdir(folder_merge2)

for file in list_file_merge1:
    file2 = file.replace("RP", "CO")
    pdf1_path = folder_merge1 + "/" + file
    pdf2_path = folder_merge2 + "/" + file2
    output_path = folders_output + "/" + file
    merge_pdfs(pdf1_path, pdf2_path, output_path)
print("Fim")


