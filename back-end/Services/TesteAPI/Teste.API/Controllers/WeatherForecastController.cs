using HtmlAgilityPack;
using iText.Html2pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Commom.FileManager.Html;
using static iText.Svg.SvgConstants;
using Commom.FileManager.Pdf;

namespace Teste.API.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

    [HttpGet("pdf")]
    public async Task<ActionResult> GetPdf()
    {        
        
        return Ok(await GeneratePDF.Generate());
    }


    [HttpGet("Auth")]
    public ActionResult GetAtuh()
    {
        return Ok("FUNFOu");
    }

    [HttpGet("Auth/Custom")]
    public ActionResult GetAuth()
    {
        return Ok("FUNFOu 222");
    }
}

class GeneratePDF
{
    public async static Task<string> Generate()
    {
        //// Criar um objeto HtmlDocument
        //var doc = new HtmlDocument();

        //using FileStream htmlSource = File.Open("Html/input.html", FileMode.Open);
        //// Carregar o arquivo HTML
        //doc.Load(htmlSource);

        //// Procurar o texto {{input.txt}} usando XPATH
        //var node = doc.DocumentNode.SelectSingleNode("//text()[contains(.,'{{input.txt}}')]");

        //// Verificar se encontrou o texto
        //if (node != null)
        //{
        //    // Substituir o texto encontrado por "Ola"
        //    node.InnerHtml = "ola";
        //    var html = doc.DocumentNode.OuterHtml;
        //    using FileStream pdfDest = File.Open("Html/output.pdf", FileMode.Create);
        //    ConverterProperties converterProperties = new ConverterProperties();
        //    HtmlConverter.ConvertToPdf(html, pdfDest, converterProperties);

        //}
        //else
        //{
        //    // Mostrar uma mensagem de erro
        //    return "Texto não encontrado.";
        //}
        // Criando uma string com valores entre chaves duplas

        //Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>(){
        //        { "contentInit", "init1" },
        //        { "contentFinal", "final1"},
        //        { "materials", new List<Dictionary<string, dynamic>>()
        //            {
        //                new Dictionary<string, dynamic>()
        //                {
        //                    {"decription", "Arroz"},
        //                    {"unit", "kg"},
        //                    {"quantity", "100" },
        //                    {"unityPrice", "R% 190,00" 
        //                    },
        //                    {"totalPrice", "R$ 500,00" }
        //                },
        //                new Dictionary<string, dynamic>()
        //                {
        //                    {"decription", "Arroz"},
        //                    {"unit", "kg"},
        //                    {"quantity", "100" },
        //                    {"unityPrice", "1,5" },
        //                    {"totalPrice", "R$ 500,00" }
        //                }
        //            }
        //        }
        //    };
    

        //await HtmlManager.CreateHtmlFiles(dic, "Files/HtmlTamplates/PurchaseTamplate", "Files/OutputsHtmls/PurchaseFile", "index.html");
        await PdfManager.ConvertHtml2Pdf("Files\\HtmlTamplates\\Nr06Tamplate\\index.html", "Files/OutputsPdfs/Nr06File.pdf");
        return "";

    }
}