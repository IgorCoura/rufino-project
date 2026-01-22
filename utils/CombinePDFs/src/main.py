import PyPDF2
import os
from unidecode import unidecode


def merge_pdf(pdf_path, pdf_writer):
    with open(pdf_path, 'rb') as pdf_file:
        pdf_reader = PyPDF2.PdfReader(pdf_file)
        for page_num in range(len(pdf_reader.pages)):
            page = pdf_reader.pages[page_num]
            pdf_writer.add_page(page)

def merge_pdfs(pdf_origin, list_pdf_merge, output_path):
    pdf_writer = PyPDF2.PdfWriter()

    # Abrir o primeiro arquivo PDF
    with open(pdf_origin, 'rb') as pdf1_file:
        pdf1_reader = PyPDF2.PdfReader(pdf1_file)
        for page_num in range(len(pdf1_reader.pages)):
            page = pdf1_reader.pages[page_num]
            pdf_writer.add_page(page)

    for pdf_merge in list_pdf_merge:
        merge_pdf(pdf_merge, pdf_writer)

    # Escrever o conteúdo combinado em um novo arquivo PDF
    with open(output_path, 'wb') as merged_pdf_file:        
        pdf_writer.write(merged_pdf_file)

    print(f'Os PDFs foram combinados com sucesso em: {output_path}')
    
def get_and_merge_pdfs(folder_name_origin, list_folders_name_merge, folders_input, folders_output):
    path_origin = folders_input + "/" + folder_name_origin
    list_file_origin = os.listdir(path_origin)

    for file in list_file_origin:
        pdf_path_origin = path_origin + "/" + file
        output_path = folders_output + "/" + file
        list_file2merge = []
        for folder_merge in list_folders_name_merge:
            path_merge = folders_input +"/"+folder_merge
            file_replaced = file.replace(folder_name_origin, folder_merge)
            pdf_path_merge = path_merge + "/" + file_replaced
            list_file2merge.append(pdf_path_merge)
        merge_pdfs(pdf_path_origin, list_file2merge, output_path)

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

folders_input =base_path + '/input'
folders_output = base_path + '/output'

# folder_merge1 = folders_input + 'RP'
# folder_merge2 = folders_input + 'CO'

get_and_merge_pdfs('RP', ['CO','RP13','CO13'], folders_input, folders_output)
get_and_merge_pdfs('RF', ['COFE'], folders_input, folders_output)

# list_file_merge1 = os.listdir(folder_merge1)
# list_file_merge2 = os.listdir(folder_merge2)

# for file in list_file_merge1:
#     file2 = file.replace("RP", "CO")
#     pdf1_path = folder_merge1 + "/" + file
#     pdf2_path = folder_merge2 + "/" + file2
#     output_path = folders_output + "/" + file
#     merge_pdfs(pdf1_path, pdf2_path, output_path)
# print("Fim")


