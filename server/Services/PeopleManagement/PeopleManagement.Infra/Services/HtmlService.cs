using PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using System.IO;
using System.Text.RegularExpressions;
using DocumentType = PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.DocumentType;

namespace PeopleManagement.Infra.Services
{
    public class HtmlService : IHtmlService
    {
        public async Task<HtmlContent> CreateHtml(DocumentType documentType, Dictionary<string, dynamic> values)
        {
            var headerPath = documentType.GetHeaderPath();
            var bodyPath = documentType.GetBodyPath();
            var footerPath = documentType.GetFooterPath();

            var headerContent = await CreateHtmlContent(headerPath, values);
            var bodyContent = await CreateHtmlContent(bodyPath, values);
            var footerContent = await CreateHtmlContent(footerPath, values);

            return HtmlContent.Create(headerContent, bodyContent, footerContent);
        }

        private static async Task<string> CreateHtmlContent(string path, Dictionary<string, dynamic> values)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"O arquivo no caminho {path}, não foi encontrado. Erro lançado em {nameof(HtmlService)}.");
            }
            var htmlContentInit = await File.ReadAllTextAsync(path);
            var htmlContentFinal = await InsertValuesInHtmlTemplate(values, htmlContentInit);
            return htmlContentFinal;
        }

        private static Task<string> InsertValuesInHtmlTemplate(Dictionary<string, dynamic> values, string html)
        {
            var result = ReplaceListTags(html, values);
            result = ReplaceDoubleBraces(result, values);
            return Task.FromResult(result);
        }

        private static string ReplaceListTags(string html, Dictionary<string, dynamic> values)
        {
            Regex regex_list = new Regex(@"<list:([^>]+)([^>]*)>([\s\S]*?)<\/list:\1>");
            return regex_list.Replace(html, m =>
            {
                string keys = m.Groups[1].Value;
                string content = m.Groups[3].Value;
                string[] listKey = keys.Split('.');

                var list = GetValueFromDictionary(values, listKey) ?? new List<Dictionary<string, dynamic>>();

                var result = "";

                foreach (var item in list)
                {
                    result += $"<div{m.Groups[2].Value}>";
                    string innerContent = ReplaceListTags(content, item);
                    result += ReplaceDoubleBraces(innerContent, item);
                    result += "</div>";
                }

                return result;
            });

        }

        private static string ReplaceDoubleBraces(string content, Dictionary<string, dynamic> values)
        {
            Regex regex = new Regex("{{(.*?)}}");
            return regex.Replace(content, m =>
            {
                string key = m.Groups[1].Value;

                string[] listKey = key.Split('.');

                var result = GetValueFromDictionary(values, listKey);

                return result ?? m.Value;
            });
        }

        private static dynamic? GetValueFromDictionary(Dictionary<string, dynamic> dict, string[] keys)
        {
            var value = dict;

            foreach (string key in keys)
            {
                if (dict.ContainsKey(key))
                {
                    if (dict[key] is not Dictionary<string, dynamic>)
                    {
                        return dict[key];
                    }
                    value = dict[key];
                }
                else
                {
                    return null;
                }
            }
            return value;
        }
    }
}
