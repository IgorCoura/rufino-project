using Commom.Domain.Exceptions;
using Commom.Tests.Models;
using MaterialPurchase.Domain.Errors;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using System.Net;

namespace MaterialPurchase.Tests.IntegrationTests
{
    public class PurchaseTests
    {
        [Fact]
        public async Task AuthorizePurchaseWithNonExistentUser()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "E16CDCFC-0EF8-4B9C-AB4D-707C175B3376");
            client.DefaultRequestHeaders.Add("role", "client");

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new ReleasePurchaseRequest(
                Guid.Parse("da9752e8-0cd6-4127-8364-c6fa7e1d8c8a"),
                constructionId,
                companyId,
                true,
                ""
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Authorize", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AuthorizePurchaseWithOutPermission()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "F363DA96-1EBB-419D-B178-3F7F3B54B863");
            client.DefaultRequestHeaders.Add("Role", "creator");

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new ReleasePurchaseRequest(
                Guid.Parse("da9752e8-0cd6-4127-8364-c6fa7e1d8c8a"),
                constructionId, 
                companyId,
                true,
                ""
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Authorize", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }


        [Fact]
        public async Task AuthorizePurchaseWithUserPedingWithHighestPriority()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "59C7F554-38E6-4C13-BB11-FE47BA08F97E");
            client.DefaultRequestHeaders.Add("role", "client");

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new ReleasePurchaseRequest(
                Guid.Parse("da9752e8-0cd6-4127-8364-c6fa7e1d8c8a"),
                constructionId, 
                companyId,
                true,
                ""
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
            client.DefaultRequestHeaders.Add("role", "client");

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new ReleasePurchaseRequest(
                Guid.Parse("ae1d0df7-deed-4e3e-85ab-82bf2453c541"),
                constructionId,
                companyId,
                true,
                ""
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Authorize", purchase);


            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{constructionId}/{purchase.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.Approved, result?.Data?.Status);
        }

        [Fact]
        public async Task UnlockPurchaseWithSuccess()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "ddf5281b-cdf7-4781-b4ad-8391f743d35c");
            client.DefaultRequestHeaders.Add("role", "supervisor");

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new ReleasePurchaseRequest(
                Guid.Parse("0c5a7011-2401-42c2-bd8a-c0b5d13739ce"),
                constructionId,
                companyId,
                true,
                ""
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Unlock", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{constructionId}/{purchase.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.Authorizing, result?.Data?.Status);
        }

        [Fact]
        public async Task UnlockPurchaseWithSuccessAsAdmin()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "4922766E-D3BA-4D4C-99B0-093D5977D41F");
            client.DefaultRequestHeaders.Add("role", "admin");

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new ReleasePurchaseRequest(
                Guid.Parse("0c5a7011-2401-42c2-bd8a-c0b5d13739ce"),
                constructionId,
                companyId,
                true,
                ""
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Unlock", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{constructionId}/{purchase.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.Authorizing, result?.Data?.Status);
        }

        [Fact]
        public async Task UnlockPurchaseWithOutSuccessAsClient()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "59C7F554-38E6-4C13-BB11-FE47BA08F97E");
            client.DefaultRequestHeaders.Add("role", "client");

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new ReleasePurchaseRequest(
                Guid.Parse("0c5a7011-2401-42c2-bd8a-c0b5d13739ce"),
                constructionId,
                companyId,
                true,
                ""
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Unlock", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmDeliveryPurchaseWithSuccessn()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "ddf5281b-cdf7-4781-b4ad-8391f743d35c");
            client.DefaultRequestHeaders.Add("role", "supervisor");

            var dateExpected = DateTime.UtcNow.AddDays(1);

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new ConfirmDeliveryDateRequest(
                constructionId,
                companyId,
                Guid.Parse("3887e6ff-13a4-4665-a8e3-14632d7dd2ce"),
                dateExpected
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Delivery/Confirm", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{constructionId}/{content?.Data?.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.WaitingDelivery, result?.Data?.Status);
            Assert.Equal(dateExpected, result?.Data?.LimitDeliveryDate);
        }

        [Fact]
        public async Task ConfirmDeliveryPurchaseWithoutAuthorize()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "59C7F554-38E6-4C13-BB11-FE47BA08F97E");
            client.DefaultRequestHeaders.Add("role", "client");

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var dateExpected = DateTime.UtcNow.AddDays(1);

            var purchase = new ConfirmDeliveryDateRequest(
                constructionId,
                companyId,
                Guid.Parse("3887e6ff-13a4-4665-a8e3-14632d7dd2ce"),
                dateExpected
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Delivery/Confirm", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmDeliveryPurchaseThatHasDeliveryProblemWithSuccessn()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "ddf5281b-cdf7-4781-b4ad-8391f743d35c");
            client.DefaultRequestHeaders.Add("role", "supervisor");

            var dateExpected = DateTime.UtcNow.AddDays(1);

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new ConfirmDeliveryDateRequest(
                constructionId, 
                companyId,
                Guid.Parse("3887e6ff-13a4-4665-a8e3-14632d7dd2ce"),
                dateExpected
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Delivery/Confirm", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{constructionId}/{content?.Data?.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.WaitingDelivery, result?.Data?.Status);
            Assert.Equal(dateExpected, result?.Data?.LimitDeliveryDate);
        }

        [Fact]
        public async Task DeliveryPartialWithSuccess()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "ddf5281b-cdf7-4781-b4ad-8391f743d35c");
            client.DefaultRequestHeaders.Add("role", "supervisor");

            var dateExpected = DateTime.UtcNow.AddDays(1);

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new ReceiveDeliveryRequest(
                constructionId,
                companyId,
                Guid.Parse("7a694ea5-a2aa-4f38-aed3-b2fbf09cc208"),
                DateTime.UtcNow,
                new ReceiveDeliveryItemRequest[]
                {
                    new ReceiveDeliveryItemRequest(
                        Guid.Parse("adcdf482-8def-492f-ae2a-6bcaa2a141e4"),
                        11),
                    new ReceiveDeliveryItemRequest(
                        Guid.Parse("a1144b14-6005-4764-875b-3b097e6ca41c"),
                        22),
                });

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Delivery/Receive", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{constructionId}/{content?.Data?.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.DeliveryProblem, result?.Data?.Status);
        }

        [Fact]
        public async Task DeliveryAllWithSuccess()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "ddf5281b-cdf7-4781-b4ad-8391f743d35c");
            client.DefaultRequestHeaders.Add("role", "supervisor");

            var dateExpected = DateTime.UtcNow.AddDays(1);

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new ReceiveDeliveryRequest(
                constructionId,
                companyId,
                Guid.Parse("7a694ea5-a2aa-4f38-aed3-b2fbf09cc208"),
                DateTime.UtcNow,
                new ReceiveDeliveryItemRequest[]
                {
                    new ReceiveDeliveryItemRequest(
                        Guid.Parse("adcdf482-8def-492f-ae2a-6bcaa2a141e4"),
                        11),
                    new ReceiveDeliveryItemRequest(
                        Guid.Parse("a1144b14-6005-4764-875b-3b097e6ca41c"),
                        44),
                });

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Delivery/Receive", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{constructionId}/{content?.Data?.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.Closed, result?.Data?.Status);
        }

        [Fact]
        public async Task DeliveryWithExcessMaterial()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "ddf5281b-cdf7-4781-b4ad-8391f743d35c");
            client.DefaultRequestHeaders.Add("role", "supervisor");

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");


            var purchase = new ReceiveDeliveryRequest(
                constructionId,
                companyId,
                Guid.Parse("7a694ea5-a2aa-4f38-aed3-b2fbf09cc208"),
                DateTime.UtcNow,
                new ReceiveDeliveryItemRequest[]
                {
                    new ReceiveDeliveryItemRequest(
                        Guid.Parse("adcdf482-8def-492f-ae2a-6bcaa2a141e4"),
                        11),
                    new ReceiveDeliveryItemRequest(
                        Guid.Parse("a1144b14-6005-4764-875b-3b097e6ca41c"),
                        555),
                });

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Delivery/Receive", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(MaterialPurchaseErrors.MaterialReceivedInvalid), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task DeliveryMaterialNotExist()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "ddf5281b-cdf7-4781-b4ad-8391f743d35c");
            client.DefaultRequestHeaders.Add("role", "supervisor");

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new ReceiveDeliveryRequest(
                constructionId,
                companyId,
                Guid.Parse("7a694ea5-a2aa-4f38-aed3-b2fbf09cc208"),
                DateTime.UtcNow,
                new ReceiveDeliveryItemRequest[]
                {
                    new ReceiveDeliveryItemRequest(
                        Guid.Parse("adcdf482-8def-492f-ae2a-6bcaa2a141e4"),
                        11),
                    new ReceiveDeliveryItemRequest(
                        Guid.Parse("0ff7fa08-efac-4057-bdd7-b2ca22b33e94"),
                        555),
                });

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Delivery/Receive", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(MaterialPurchaseErrors.MaterialReceivedInvalid), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task CancelPurchaseWithSuccess()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "4922766E-D3BA-4D4C-99B0-093D5977D41F");
            client.DefaultRequestHeaders.Add("role", "admin");

            var constructionId = Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7");
            var companyId = Guid.Parse("3551e82d-3dc4-4017-a9d7-b062550409fb");

            var purchase = new CancelPurchaseRequest(
                     constructionId,
                     companyId,
                     Guid.Parse("da9752e8-0cd6-4127-8364-c6fa7e1d8c8a"),
                     ""
                    );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Cancel/Admin", purchase);


            //Asssert 
            var cont = response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{constructionId}/{content?.Data?.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.Cancelled, result?.Data?.Status);
            Assert.Equal(purchase.Comment, result?.Data?.AuthorizationUserGroups.OrderBy(x => x.Priority).ToArray()[1].UserAuthorizations.ToArray()[0].Comment);
        }

    }
}
