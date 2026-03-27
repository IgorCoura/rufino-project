using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace PeopleManagement.Infra.Services
{
    public static partial class HtmlService
    {
        public static string InsertValuesInHtmlTemplate(JsonNode? values, string html)
        {
            var result = ReplaceListTags(html, values);
            result = ReplaceDoubleBraces(result, values);
            return result;
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

                var sb = new StringBuilder(jsonArray.Count * content.Length);

                foreach (var item in jsonArray)
                {
                    sb.Append("<div").Append(m.Groups[2].Value).Append('>');
                    string innerContent = ReplaceListTags(content, item);
                    sb.Append(ReplaceDoubleBraces(innerContent, item));
                    sb.Append("</div>");
                }

                return sb.ToString();
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
            if (json == null || keys.Length == 0)
                return null;

            JsonNode? current = json;

            for (int i = 0; i < keys.Length; i++)
            {
                if (current is not JsonObject obj)
                    return null;

                JsonNode? value = null;
                foreach (var kvp in obj)
                {
                    if (string.Equals(kvp.Key, keys[i], StringComparison.OrdinalIgnoreCase))
                    {
                        value = kvp.Value;
                        break;
                    }
                }

                if (value == null)
                    return null;

                current = value;
            }

            return current;
        }

        public record HtmlContent(string Header, string Body, string Footer);

        [GeneratedRegex(@"<list:([^>]+)([^>]*)>([\s\S]*?)<\/list:\1>")]
        private static partial Regex HtmlListRegex();
        [GeneratedRegex("{{(.*?)}}")]
        private static partial Regex HtmlParamRegex();
    }
}
