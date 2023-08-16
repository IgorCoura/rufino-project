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
        public static Task<string> InsertValuesInHtmlTemplate(Dictionary<string, dynamic> values, string pathHtmlTemplate)
        {
            using StreamReader sr = new StreamReader(pathHtmlTemplate);
            string html = sr.ReadToEnd();

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

