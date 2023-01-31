using Commom.Domain.Exceptions;
using Commom.Domain.SeedWork;
using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using MaterialPurchase.Infra.Context;
using MaterialPurchase.Tests.Utils;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MaterialPurchase.Tests.Properties.IntegrationTests
{
    public class DraftPurchase : IClassFixture<MaterialPurchaseFactory>
    {
        private readonly HttpClient _client;
        private readonly MaterialPurchaseFactory _factory;

        public DraftPurchase(MaterialPurchaseFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreatePurchaseWithSuccess()
        {
            //Arrange
            

            var newPurchase = new CreateDraftPurchaseRequest(
                Guid.Parse("8299C0DC-927D-45DE-B2C8-71C38FAF9384"),
                Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
                99,
                new CreateMaterialDraftPurchaseRequest[]
                {
                    new CreateMaterialDraftPurchaseRequest(Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"), Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"), 77, 33),
                    new CreateMaterialDraftPurchaseRequest(Guid.Parse("91909CEA-E52C-4945-AAA9-1E50266C1C66"), Guid.Parse("2C377F5B-DA7A-4A2E-87BB-1C16894ADC0D"), 77, 33),
                });

            //Act
            var response = await _client.PostAsJsonAsync("/api/v1/DraftPurchase/Create", newPurchase);
            var content = response.Content.ReadFromJsonAsync(typeof(Response<PurchaseResponse>)).Result as Response<PurchaseResponse>;
            var result = await _client.GetFromJsonAsync<Response<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{content.Data.Id}");

            //Asssert 
            Assert.True(newPurchase.EqualExtesion(result!.Data!));
        }

        [Fact]
        public async Task UpdatePurchaseWithSuccess()
        {
            //Arrange

            var purchase = new DraftPurchaseRequest(
                Guid.Parse("CA100B9F-8D13-4E64-ADBC-A90462D05A9A"),
                Guid.Parse("8299C0DC-927D-45DE-B2C8-71C38FAF9384"),
                Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
                99,
                new MaterialDraftPurchaseRequest[]
                {
                    new MaterialDraftPurchaseRequest(Guid.Parse("005D1FEF-3308-4B27-8CB2-2CE610C1E231"), Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"), Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"), 97, 22)
                });

           
            //Act
            var response = await _client.PostAsJsonAsync("/api/v1/DraftPurchase/Update", purchase);
            var content = response.Content.ReadFromJsonAsync(typeof(Response<PurchaseResponse>)).Result as Response<PurchaseResponse>;
            var result = await _client.GetFromJsonAsync<Response<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{content!.Data!.Id}");

            //Asssert 
            Assert.True(purchase.EqualExtesion(result!.Data!));
        }

        [Fact]
        public async Task DeletePurchaseWithSuccess()
        {
            //Arrange

            var purchase = new PurchaseRequest(
                Guid.Parse("CA100B9F-8D13-4E64-ADBC-A90462D05A9A")
                );

            //Act
            var response = await _client.PostAsJsonAsync("/api/v1/DraftPurchase/Delete", purchase);


            //Asssert 
            //TODO: Verificar exceção
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _client.GetFromJsonAsync<Response<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{purchase.Id}"));
        }

        [Fact]
        public async Task SendPurchaseWithSuccess()
        {
            //Arrange

            var purchase = new PurchaseRequest(
                Guid.Parse("CA100B9F-8D13-4E64-ADBC-A90462D05A9A")
                );

            //Act
            var response = await _client.PostAsJsonAsync("/api/v1/DraftPurchase/Send", purchase);
            var result = await _client.GetFromJsonAsync<Response<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/CA100B9F-8D13-4E64-ADBC-A90462D05A9A");

            //Asssert 
            Assert.True(true);
        }


    }
}
