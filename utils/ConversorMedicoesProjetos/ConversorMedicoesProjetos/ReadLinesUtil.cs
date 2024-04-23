using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversorMedicoesProjetos
{
    public class ReadLinesUtil
    {
        const string DEFAULT_DATA_LOCAL = "aux_tabs\\";
        public static string GetDirectoryCSV()
        {
            while (true)
            {
                Console.WriteLine("Informe pasta onde está contido os arquivos csv: ");
                var value = Console.ReadLine();
                if (value is null || value == "")
                {
                    Console.WriteLine("Informação invalida. Tente novamente.\n");
                    continue;
                }

                try
                {
                    var directory = Path.Combine(Directory.GetCurrentDirectory(), value);
                    string[] files = Directory.GetFiles(directory);
                    var isCsv = files.Any(x => CheckExtesion(".csv", x) == false);

                    if (files.Length > 0 && !isCsv)
                    {
                        return value;
                    }
                    throw new Exception();
                }
                catch {
                    Console.WriteLine("Erro: Não existem arquivos CSV ou pelo menos um dos arquivos não está no formato CSV.Tente novamente.\n");
                }
                
            }
        }

        public static string GetAuxTab()
        {
            while (true)
            {
                Console.WriteLine("Informe a tabela auxiliar: ");
                var value = Console.ReadLine();
                if (value is null || value == "")
                {
                    Console.WriteLine("Informação invalida. Tente novamente.\n");
                    continue;
                }

                value = DEFAULT_DATA_LOCAL + value;
                string extensao = Path.GetExtension(value);
                bool hasCsv = string.Equals(extensao, ".csv", StringComparison.OrdinalIgnoreCase);

                bool fileExist = File.Exists(value);

                if (hasCsv && fileExist)
                {
                    return value;
                }

                Console.WriteLine("Arquivo não existe ou não é csv. Tente novamente.\n");
            }
        }

        private static bool CheckExtesion(string extesion, string value)
        {
            string extensao = Path.GetExtension(value);
            return string.Equals(extensao, extesion, StringComparison.OrdinalIgnoreCase);
        }
    }
}
