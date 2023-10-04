using CsvHelper.Configuration;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace DocsGenerator
{
    public class CsvManager
    {
        public static List<Dictionary<string, dynamic>> ConvertCsv2Dictionary(string csvFilePath)
        {
            
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Encoding = Encoding.UTF8, // Our file uses UTF-8 encoding.
                Delimiter = ";" // The delimiter is a comma.
            };


            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, csvConfig);

            csv.Read();
            csv.ReadHeader();
            string[]? headerRow = csv.Context.Reader.HeaderRecord;

            List<Dictionary<string, dynamic>> listDictionary =  new();

            if (headerRow is not null)
            {
                while (csv.Read())
                {
                    var dictionary = new Dictionary<string, dynamic>();
                    foreach (var row in headerRow)
                    {
                        if(String.IsNullOrEmpty(row))
                            continue;
                        dictionary.Add(row, csv.GetField<string>(row)!);
                    }
                    bool allEmpty = dictionary.All(item => string.IsNullOrWhiteSpace(item.Value));
                    if (allEmpty)
                        continue;
                    listDictionary.Add(dictionary);
                }
                
            }

            return listDictionary;
        }
    }

}
