using iText.Kernel.XMP.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocsGenerator
{
    public static class ReadLinesUtil
    {
        const string FILE_TEMPLATE = "\\index.html";
        const string DEFAULT_TEMPLATE_LOCAL = "templates\\";
        const string DEFAULT_DATA_LOCAL = "data\\";

        public static bool GetIfContinue()
        {
            while (true)
            {
                Console.WriteLine("Deseja gerar outro documento Sim(S)/Não(N)/Sair(0): ");
                string[] options = new string[] { "sim", "s", "não", "n", "sair", "0" };
                var value = Console.ReadLine();
                if (value is null || value == "" || options.Contains(value.ToLower().Trim()) is false)
                {    
                    Console.WriteLine("Opção invalida. Tente novamente.\n");
                    continue;
                }
                value = value.ToLower().Trim();
                if (value == "s" || value == "sim")
                    return true;
                return false;
            }
        }

        public static string GetPathTamplate()
        {
            while (true)
            {
                Console.WriteLine("Informe o tamplate html: ");
                var value = Console.ReadLine();
                if (value is null || value == "")
                {
                    Console.WriteLine("Informação invalida. Tente novamente.\n");
                    continue;
                }

                var path = DEFAULT_TEMPLATE_LOCAL + value;

                bool fileExist = File.Exists(path + FILE_TEMPLATE);

                if (fileExist)
                {
                    return path;
                }

                Console.WriteLine("Arquivo não existe ou não é html. Tente novamente.\n");
            }
        }

        public static string GetPathCsv()
        {
            while (true)
            {
                Console.WriteLine("Informe o arquivo csv: ");
                var value = Console.ReadLine();
                if (value is null || value == "")
                {
                    Console.WriteLine("Informação invalida. Tente novamente.\n");
                    continue;
                }

                value = DEFAULT_DATA_LOCAL + value;
                string extensao = Path.GetExtension(value);
                bool hasHtml = string.Equals(extensao, ".csv", StringComparison.OrdinalIgnoreCase);

                bool fileExist = File.Exists(value);

                if (hasHtml && fileExist)
                {
                    return value;
                }

                Console.WriteLine("Arquivo não existe ou não é csv. Tente novamente.\n");
            }
        }

        public static string GetFileNamePdf()
        {
            while (true)
            {
                Console.WriteLine("Informe o nome que deseja para o Pdf: ");
                var value = Console.ReadLine();
                
                if (value is null || value == "")
                {
                    Console.WriteLine("Informação invalida. Tente novamente.\n");
                    continue;
                }

                return value;          
            }
        }
    }
}
