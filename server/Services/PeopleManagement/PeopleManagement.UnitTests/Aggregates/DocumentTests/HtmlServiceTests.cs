using System.Text.Json.Nodes;
using PeopleManagement.Infra.Services;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    public class HtmlServiceTests
    {
        [Fact]
        public void InsertValues_ReplacesKnownPlaceholderWithValue()
        {
            var values = JsonNode.Parse("""{"Name": "João"}""");
            var html = "<p>{{Name}}</p>";

            var result = HtmlService.InsertValuesInHtmlTemplate(values, html);

            Assert.Equal("<p>João</p>", result);
        }

        [Fact]
        public void InsertValues_ReplacesUnknownPlaceholderWithEmpty()
        {
            var values = JsonNode.Parse("""{"Name": "João"}""");
            var html = "<p>{{MissingField}}</p>";

            var result = HtmlService.InsertValuesInHtmlTemplate(values, html);

            Assert.Equal("<p></p>", result);
        }

        [Fact]
        public void InsertValues_ReplacesNestedUnknownPlaceholderWithEmpty()
        {
            var values = JsonNode.Parse("""{"Employee": {"Name": "João"}}""");
            var html = "<p>{{Employee.Address}}</p>";

            var result = HtmlService.InsertValuesInHtmlTemplate(values, html);

            Assert.Equal("<p></p>", result);
        }

        [Fact]
        public void InsertValues_ReplacesAllUnknownPlaceholdersWithEmpty()
        {
            var values = JsonNode.Parse("""{}""");
            var html = "<p>{{Field1}} - {{Field2}} - {{Field3}}</p>";

            var result = HtmlService.InsertValuesInHtmlTemplate(values, html);

            Assert.Equal("<p> -  - </p>", result);
        }

        [Fact]
        public void InsertValues_MixedKnownAndUnknownPlaceholders()
        {
            var values = JsonNode.Parse("""{"Name": "Maria", "Company": {"Name": "Rufino"}}""");
            var html = "<p>{{Name}} - {{Age}} - {{Company.Name}} - {{Company.Cnpj}}</p>";

            var result = HtmlService.InsertValuesInHtmlTemplate(values, html);

            Assert.Equal("<p>Maria -  - Rufino - </p>", result);
        }

        [Fact]
        public void InsertValues_NullValues_ReplacesAllPlaceholdersWithEmpty()
        {
            var html = "<p>{{Name}}</p>";

            var result = HtmlService.InsertValuesInHtmlTemplate(null, html);

            Assert.Equal("<p></p>", result);
        }

        [Fact]
        public void InsertValues_NoPlaceholders_ReturnsOriginalHtml()
        {
            var values = JsonNode.Parse("""{"Name": "João"}""");
            var html = "<p>Sem placeholders</p>";

            var result = HtmlService.InsertValuesInHtmlTemplate(values, html);

            Assert.Equal("<p>Sem placeholders</p>", result);
        }
    }
}
