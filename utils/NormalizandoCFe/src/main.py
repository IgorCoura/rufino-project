import csv
import os
import requests
from bs4 import BeautifulSoup
from datetime import datetime

def read_fourth_column(input_file):
    fourth_column = []
    try:
        with open(input_file, mode='r', encoding='utf-8') as file:
            csv_reader = csv.reader(file)
            for row in csv_reader:
                if len(row) >= 4:  # Ensure the row has at least 4 columns
                    fourth_column.append(row[3])
    except FileNotFoundError:
        print(f"File not found: {input_file}")
    except Exception as e:
        print(f"An error occurred: {e}")
    return fourth_column

def process_column_data(fourth_column_data):
        processed_data = []
        for line in fourth_column_data:
            if not line.startswith("http"):  # Ignore lines that are URLs
                parts = line.split("|")
                if len(parts) >= 4:  # Ensure there are at least 4 parts
                    codigo = parts[0]
                    if codigo.startswith("CFe"):
                        codigo = codigo.replace("CFe", "")
                    
                    # Convert date format
                    raw_date = parts[1]
                    try:
                        date_obj = datetime.strptime(raw_date, "%Y%m%d%H%M%S")
                        data = date_obj.strftime("%Y/%m/%d - %H:%M:%S")
                    except ValueError:
                        data = raw_date  # Keep the original value if conversion fails
                    
                    valor = parts[2]
                    valor = valor.replace(".", ",")
                    consumer = parts[3]
                    fonte = line
                    tipo = "CF-e SAT"
                    processed_data.append((tipo, codigo, data, valor, consumer, fonte))
        return processed_data
    
def fetch_and_parse_urls(fourth_column_data):
        url_data = []
        for line in fourth_column_data:
            if line.startswith("http"):  # Process only lines that are URLs
                try:
                    response = requests.get(line)
                    fonte = line
                    response.raise_for_status()
                    soup = BeautifulSoup(response.text, 'html.parser')

                    # Extract VALOR
                    valor_element = soup.find(class_="totalNumb txtMax")
                    valor = valor_element.text.strip() if valor_element else None

                    # Extract CODIGO
                    codigo_element = soup.find(class_="chave") 
                    codigo = codigo_element.text.strip() if codigo_element else None
                    codigo = codigo.replace(" ", "")

                    # Extract DATA
                    ul_element = soup.find(id="infos")
                    if ul_element:
                        li_text = ul_element.find("li").text if ul_element.find("li") else ""
                        data_start = li_text.find("Emissão: ")
                        if data_start != -1:
                            data_end = li_text.find("- Via Consumidor", data_start)  # Find the end of the date and time
                            data = li_text[data_start + 9:data_end].strip()  # Extract date and time
                            try:
                                date_obj = datetime.strptime(data, "%d/%m/%Y %H:%M:%S")
                                data = date_obj.strftime("%Y/%m/%d - %H:%M:%S")
                            except ValueError:
                                pass  # Keep the original value if conversion fails
                        else:
                            data = None
                    else:
                        data = None
                        
                    # Extract Consumer
                    consumer = ""
                    tipo = "NFC-e"
                    url_data.append((tipo, f"\"{codigo}\"", data, valor, consumer, fonte))
                except Exception as e:
                    print(f"Failed to process URL {line}: {e}")
        return url_data
    
def save_to_csv(data, output_file):
    os.makedirs(os.path.dirname(output_file), exist_ok=True)
    try:
        with open(output_file, mode='w', newline='', encoding='utf-8') as file:
            csv_writer = csv.writer(file, delimiter=';')  # Use ";" as the delimiter
            csv_writer.writerow(["TIPO","CODIGO", "DATA", "VALOR", "CONSUMIDOR","FONTE"])
            csv_writer.writerows(data)
    except Exception as e:
        print(f"An error occurred while writing to CSV: {e}")

if __name__ == "__main__":
    
    print("INICIO")
      
    input_path = os.path.join(os.getcwd(), "input", "CUPONS_FISCAIS.csv")
    output_path = os.path.join(os.getcwd(), "output", "NFC-e_CF-e.csv")
    
    
    fourth_column_data = read_fourth_column(input_path)
    processed_data = process_column_data(fourth_column_data)
    url_data = fetch_and_parse_urls(fourth_column_data)

    combined_data = processed_data + url_data  # Combine both lists
    
    save_to_csv(combined_data, output_path)
   
    print("FIM")