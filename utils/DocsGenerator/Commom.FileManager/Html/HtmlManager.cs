using Commom.FileManager.Files;
using iText.Kernel.XMP.Impl.XPath;
using iText.Layout.Element;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static iText.Svg.SvgConstants;

namespace Commom.FileManager.Html
{
    public class HtmlManager
    {

        public async static Task<HtmlContent> CreateHtmlContent(Dictionary<string, dynamic> values, string pathOrigin)
        {
            const string tempPath = "Temp";
            const string nameFileBody = "index.html";
            const string nameFileHeader = "header.html";
            const string nameFileFooter = "footer.html";

            var pathHtmlBody = Path.Combine(pathOrigin, nameFileBody);
            var pathHtmlHeader = Path.Combine(pathOrigin, nameFileHeader);
            var pathHtmlFooter = Path.Combine(pathOrigin, nameFileFooter);

            var tempPathLocal = Path.Combine(tempPath, Guid.NewGuid().ToString());

            await CopyDirectory.Copy(pathOrigin, tempPathLocal);
             
            var pathDestinyHtml = Path.Combine(tempPathLocal, nameFileBody);
            string htmlStringBody = await File.ReadAllTextAsync(pathHtmlBody);
            var htmlBody = await InsertValuesInHtmlTemplate(values, htmlStringBody);
            await File.WriteAllTextAsync(pathDestinyHtml, htmlBody);

            try
            {
                string htmlStringHeader = await File.ReadAllTextAsync(pathHtmlHeader);
                var htmlHeader = await InsertValuesInHtmlTemplate(values, htmlStringHeader);

                string htmlStringFooter = await File.ReadAllTextAsync(pathHtmlFooter);
                var htmlFooter = await InsertValuesInHtmlTemplate(values, htmlStringFooter);

                var result = new HtmlContent(htmlHeader, pathDestinyHtml, htmlFooter);

                return result;
            }
            catch
            {
                var result = new HtmlContent("", pathDestinyHtml, "");

                return result;
            }

            
        }
        
        public static Task<string> InsertValuesInHtmlTemplate(Dictionary<string, dynamic> values, string html)
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

        private static string ReplaceDoubleBraces(string content, Dictionary<string, dynamic>  values)
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

