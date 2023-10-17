using Commom.FileManager.Html;
using Commom.FileManager.Pdf;
using DocsGenerator;
using iText.Kernel.XMP.Impl.XPath;
using iText.Layout.Element;
using System.Collections.Generic;

Console.WriteLine("Bem Vindo ao gerador de documentos.");
Directory.SetCurrentDirectory(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName);
Console.WriteLine("Diretorio utilizado: " + Directory.GetCurrentDirectory());
await PdfManager.DownloadBrowser();

while (true)
{
    const string tempPath = "Temp";
    const string outputPath = "OutputsPdfs";

    var htmlTemplatePath = ReadLinesUtil.GetPathTamplate();
    var directoryHtmlPath = Path.GetDirectoryName(htmlTemplatePath)!;
    var htmlFileName = Path.GetFileName(htmlTemplatePath)!;
    var pdfPrefix = ReadLinesUtil.GetFileNamePdf();

    var pathCsv = ReadLinesUtil.GetPathCsv();

    var listDictinary = CsvManager.ConvertCsv2Dictionary(pathCsv);

    Console.Write("Criando documentos... ");
    using var progress = new ProgressBar(listDictinary.Count());

    Task[] tasks = new Task[listDictinary.Count()];

    for (int i = 0; i < listDictinary.Count(); i++)
    {

        tasks[i] = createDocuments(progress, listDictinary[i], pdfPrefix, directoryHtmlPath, tempPath, htmlFileName, outputPath);

    }

    Task.WaitAll(tasks);

    Directory.Delete(tempPath, true);
    Console.WriteLine("Fim.");
    progress.Dispose();

    var op = ReadLinesUtil.GetIfContinue();
    if(op is false)
    {
        return;
    }

}

async Task createDocuments(ProgressBar progress, Dictionary<string, dynamic> dictionary, string pdfPrefix, string directoryHtmlPath, string tempPath, string htmlFileName, string outputPath)
{

    string fileName = dictionary.First().Value;
    string pdfFileName = pdfPrefix + fileName + ".pdf";
    var index = 0;

    var tempPathLocal = Path.Combine(tempPath, Guid.NewGuid().ToString());

    while (File.Exists(pdfFileName))
    {
        index++;
        pdfFileName += index.ToString();
    }

    try
    {
        await HtmlManager.CreateHtmlFiles(dictionary, directoryHtmlPath, tempPathLocal, htmlFileName);
    }
    catch(Exception ex)
    {
        Console.WriteLine($"Erro ao inserir dados do {fileName} no Html. Message: {ex.Message}");
    }

    try
    {
        
        await PdfManager.ConvertHtml2Pdf(tempPathLocal, outputPath, htmlFileName, pdfFileName);
    }
    catch(Exception ex)
    {
        Console.WriteLine($"Erro ao converter o Html em Pdf dos dados do {fileName}. Message: {ex.Message}");
    }

    progress.Report();
}