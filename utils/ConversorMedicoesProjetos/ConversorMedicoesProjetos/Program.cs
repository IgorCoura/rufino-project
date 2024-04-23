// See https://aka.ms/new-console-template for more information
using ConversorMedicoesProjetos;
using System.IO;

string filePathWrite = $"Output//{Guid.NewGuid()}.csv";

Console.WriteLine("Bem-vindo, Conversor de medidas feitas com OrcaCad.");

Directory.SetCurrentDirectory(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName);
Console.WriteLine("Diretorio utilizado: " + Directory.GetCurrentDirectory());

var directoryFiles = ReadLinesUtil.GetDirectoryCSV();
var auxTab = ReadLinesUtil.GetAuxTab();

string[] filesCsv = Directory.GetFiles(directoryFiles);

var allLineAuxTab = await File.ReadAllLinesAsync(auxTab);

if (!Directory.Exists(Path.GetDirectoryName(filePathWrite)))
{
    Directory.CreateDirectory(Path.GetDirectoryName(filePathWrite)!);
}

if (!File.Exists(filePathWrite))
    File.Create(filePathWrite).Close();

var list = new List<string>();

foreach (string file in filesCsv)
{
    var allLines = await File.ReadAllLinesAsync(file);
    allLines.Select(x =>
    {
        return x.Trim().Replace("\"", "").Replace("\\", "");
    });

    var medicoes = new Medicoes(Path.GetFileNameWithoutExtension(file));

    foreach(var line in allLines)
    {
        var items = line.Split(';');

        if (string.IsNullOrEmpty(items[6].Trim()) || string.Equals(items[6].Trim().ToLower(), "chave"))
        {
            continue;
        }

        var medicao = new Medicao(items[6], items[1], items[3], items[4]);
        medicao.SetCategoria(allLineAuxTab);

        medicoes.Add(medicao);
    }

    list.AddRange(medicoes.ToCSV());
}

File.WriteAllLines(filePathWrite, list);



