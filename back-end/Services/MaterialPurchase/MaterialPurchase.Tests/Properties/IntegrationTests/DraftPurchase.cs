using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace MaterialPurchase.Tests.Properties.IntegrationTests
{
    public class DraftPurchase
    {
        
        [Fact]
        public async Task CreatePurchaseWithSuccess()
        {
            //Arrange
            var application = new MaterialPurchaseApplication();
            var client = application.CreateClient();
            var context = application.GetContext();
            var repository = new BaseRepository<Construction>(context);

            //Act

            var result = await client.GetFromJsonAsync<Response<IEnumerable<Construction>>>("/api/v1/DraftPurchase");

            var result1 = await repository.GetDataAsync(include: i => i.Include(x => x.PurchasingAuthorizationUserGroups).ThenInclude(x => x.UserAuthorizations));

            //Asssert 
            Assert.NotNull(result);
        }
    }
}
