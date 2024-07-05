using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DocumentType = PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.DocumentType;

namespace PeopleManagement.Infra.Services
{
    public static class HtmlService
    {
        public static async Task<HtmlContent> CreateTemporaryHtmlTemplate(string bodyContent, DocumentType type, string values, string templatesSourcePath, CancellationToken cancellationToken = default)
        {
            var json = JsonValue.Parse(values);

            var header = await CreateHtmlContent(json, type.GetHeaderPath(templatesSourcePath));
            var body = await InsertValuesInHtmlTemplate(json, bodyContent);
            var footer = await CreateHtmlContent(json, type.GetFooterPath(templatesSourcePath));

            return new HtmlContent(header, body, footer);
        }
            

        private static async Task<string> CreateHtmlContent(JsonNode? values, string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"O arquivo no caminho {path}, não foi encontrado. Erro lançado em {nameof(HtmlService)}.");
            }
            var htmlContentInit = await File.ReadAllTextAsync(path);
            var htmlContentFinal = await InsertValuesInHtmlTemplate(values, htmlContentInit);
            return htmlContentFinal;
        }

        private static Task<string> InsertValuesInHtmlTemplate(JsonNode? values, string html)
        {
            var result = ReplaceListTags(html, values);
            result = ReplaceDoubleBraces(result, values);
            return Task.FromResult(result);
        }

        private static string ReplaceListTags(string html, JsonNode? values)
        {
            Regex regex_list = new Regex(@"<list:([^>]+)([^>]*)>([\s\S]*?)<\/list:\1>");
            return regex_list.Replace(html, m =>
            {
                string keysString = m.Groups[1].Value;
                string content = m.Groups[3].Value;
                string[] keys = keysString.Split('.');
              

                var jsonArray = GetValueFromJson(keys, values) as JsonArray ?? [];

                var result = "";

                foreach (var item in jsonArray)
                {
                    result += $"<div{m.Groups[2].Value}>";
                    string innerContent = ReplaceListTags(content, item);
                    result += ReplaceDoubleBraces(innerContent, item);
                    result += "</div>";
                }

                return result;
            });

        }

        private static string ReplaceDoubleBraces(string content, JsonNode? values)
        {
            Regex regex = new Regex("{{(.*?)}}");
            return regex.Replace(content, m =>
            {
                string keysString = m.Groups[1].Value;

                string[] keys = keysString.Split('.');

                var result = GetValueFromJson(keys, values)?.ToString();

                return result ?? m.Value;
            });
        }

        private static JsonNode? GetValueFromJson(string[] keys, JsonNode? json)
        {
            if (json == null)
                return null;

            foreach (string key in keys)
            {
                try
                {
                    var value = json[key];

                    if(value == null)
                    {
                        return null;
                    }

                    if(value is JsonObject)
                    {
                        return GetValueFromJson(keys.Skip(1).ToArray(), (JsonObject)value);
                    }

                    return value;
                }
                catch
                {
                    continue;
                }
            }
            return null;
        }

        public record HtmlContent(string Header, string Body, string Footer);
    }
}
