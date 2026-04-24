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
            result = ReplaceEmptyTags(result, values);
            result = ReplaceHasTags(result, values);
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

                var attrs = m.Groups[2].Value;
                var hasAttrs = !string.IsNullOrWhiteSpace(attrs);

                foreach (var item in jsonArray)
                {
                    if (hasAttrs)
                        sb.Append("<div").Append(attrs).Append('>');
                    string innerContent = ReplaceListTags(content, item);
                    sb.Append(ReplaceDoubleBraces(innerContent, item, keepIfMissing: true));
                    if (hasAttrs)
                        sb.Append("</div>");
                }

                return sb.ToString();
            });
        }

        private static string ReplaceEmptyTags(string html, JsonNode? values)
        {
            Regex regex = HtmlEmptyRegex();
            return regex.Replace(html, m =>
            {
                string keysString = m.Groups[1].Value;
                string content = m.Groups[3].Value;
                string[] keys = keysString.Split('.');

                var node = GetValueFromJson(keys, values);
                var jsonArray = node as JsonArray;
                var isEmpty = jsonArray == null || jsonArray.Count == 0;

                if (!isEmpty)
                    return string.Empty;

                var attrs = m.Groups[2].Value;
                var result = ReplaceDoubleBraces(content, values);

                if (!string.IsNullOrWhiteSpace(attrs))
                    return $"<div{attrs}>{result}</div>";

                return result;
            });
        }

        private static string ReplaceHasTags(string html, JsonNode? values)
        {
            Regex regex = HtmlHasRegex();
            return regex.Replace(html, m =>
            {
                string keysString = m.Groups[1].Value;
                string content = m.Groups[2].Value;
                string[] keys = keysString.Split('.');

                var node = GetValueFromJson(keys, values);
                var jsonArray = node as JsonArray;

                if (jsonArray != null && jsonArray.Count > 0)
                    return ReplaceDoubleBraces(content, values);

                return string.Empty;
            });
        }

        private static string ReplaceDoubleBraces(string content, JsonNode? values, bool keepIfMissing = false)
        {
            Regex regex = HtmlParamRegex();
            return regex.Replace(content, m =>
            {
                string keysString = m.Groups[1].Value;
                string[] keys = keysString.Split('.');
                var result = GetValueFromJson(keys, values)?.ToString();
                if (result != null)
                    return result;
                return keepIfMissing ? m.Value : string.Empty;
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
        [GeneratedRegex(@"<empty:([^>]+)([^>]*)>([\s\S]*?)<\/empty:\1>")]
        private static partial Regex HtmlEmptyRegex();
        [GeneratedRegex(@"<has:([^>]+)>([\s\S]*?)<\/has:\1>")]
        private static partial Regex HtmlHasRegex();
        [GeneratedRegex("{{(.*?)}}")]
        private static partial Regex HtmlParamRegex();
    }
}
