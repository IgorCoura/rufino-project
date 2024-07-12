using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace PeopleManagement.Infra.Services
{
    public static partial class HtmlService
    {
        public static Task<string> InsertValuesInHtmlTemplate(JsonNode? values, string html)
        {
            var result = ReplaceListTags(html, values);
            result = ReplaceDoubleBraces(result, values);
            return Task.FromResult(result);
        }

        private static string ReplaceListTags(string html, JsonNode? values)
        {
            Regex regex_list = HtmlListRegex();
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
            Regex regex = HtmlParamRegex();
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

                    if(value is JsonObject jsonValue)
                    {
                        return GetValueFromJson(keys.Skip(1).ToArray(), jsonValue);
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

        [GeneratedRegex(@"<list:([^>]+)([^>]*)>([\s\S]*?)<\/list:\1>")]
        private static partial Regex HtmlListRegex();
        [GeneratedRegex("{{(.*?)}}")]
        private static partial Regex HtmlParamRegex();
    }
}
