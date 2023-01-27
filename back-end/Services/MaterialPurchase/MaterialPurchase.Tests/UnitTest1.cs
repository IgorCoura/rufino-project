using Microsoft.AspNetCore.Mvc.Testing;

namespace MaterialPurchase.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var application = new MaterialPurchaseApplication();

            var client = application.CreateClient();
            var result1 = await client.GetAsync("/api/v1/Teste");
            var result = await client.GetAsync("/api/v1/DraftPurchase");
            var body = result.Content.ReadAsStringAsync();
            Assert.NotNull(body);
        }
    }
}