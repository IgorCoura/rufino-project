using Commom.Domain.Exceptions;
using MaterialPurchase.Domain.Consts;
using MaterialPurchase.Domain.Errors;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using MaterialPurchase.Tests.Models;
using System.Net;

namespace MaterialPurchase.Tests.Properties.IntegrationTests
{
    public class Purchase
    {
        [Fact]
        public async Task AuthorizePurchaseWithNonExistentUser()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "4922766E-D3BA-4D4C-99B0-093D5977D41F");
            client.DefaultRequestHeaders.Add("role", MaterialPurchaseAuthorizationId.AuthorizePurchase);

            var purchase = new PurchaseRequest(
                Guid.Parse("da9752e8-0cd6-4127-8364-c6fa7e1d8c8a")
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Authorize", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(MaterialPurchaseErrors.AuthorizationInvalid), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task AuthorizePurchaseWithSuccess()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "59C7F554-38E6-4C13-BB11-FE47BA08F97E");
            client.DefaultRequestHeaders.Add("role", MaterialPurchaseAuthorizationId.AuthorizePurchase);

            var purchase = new PurchaseRequest(
                Guid.Parse("da9752e8-0cd6-4127-8364-c6fa7e1d8c8a")
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Authorize", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            client.DefaultRequestHeaders.Remove("Role");
            client.DefaultRequestHeaders.Add("Role", MaterialPurchaseAuthorizationId.GetPurchaseComplete);
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{purchase.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.Approved, result?.Data?.Status);
        }

        [Fact]
        public async Task UnlockPurchaseWithSuccess()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "59C7F554-38E6-4C13-BB11-FE47BA08F97E");
            client.DefaultRequestHeaders.Add("role", MaterialPurchaseAuthorizationId.UnlockPurchase);

            var purchase = new PurchaseRequest(
                Guid.Parse("0c5a7011-2401-42c2-bd8a-c0b5d13739ce")
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Unlock", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            client.DefaultRequestHeaders.Remove("Role");
            client.DefaultRequestHeaders.Add("Role", MaterialPurchaseAuthorizationId.GetPurchaseComplete);
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{purchase.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.Authorizing, result?.Data?.Status);
        }
        
    }
}
