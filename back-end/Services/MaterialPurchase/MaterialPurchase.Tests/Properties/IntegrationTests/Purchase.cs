using Commom.Domain.Exceptions;
using MaterialPurchase.Domain.Consts;
using MaterialPurchase.Domain.Errors;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using MaterialPurchase.Tests.Models;
using Microsoft.EntityFrameworkCore;
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

            client.DefaultRequestHeaders.Add("Sid", "E16CDCFC-0EF8-4B9C-AB4D-707C175B3376");
            client.DefaultRequestHeaders.Add("role", "client");

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
        public async Task AuthorizePurchaseWithUserPedingWithHighestPriority()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "59C7F554-38E6-4C13-BB11-FE47BA08F97E");
            client.DefaultRequestHeaders.Add("role", "client");

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
            client.DefaultRequestHeaders.Add("role", "client");

            var purchase = new PurchaseRequest(
                Guid.Parse("ae1d0df7-deed-4e3e-85ab-82bf2453c541")
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Authorize", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
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

            client.DefaultRequestHeaders.Add("Sid", "ddf5281b-cdf7-4781-b4ad-8391f743d35c");
            client.DefaultRequestHeaders.Add("role", "client");

            var purchase = new PurchaseRequest(
                Guid.Parse("0c5a7011-2401-42c2-bd8a-c0b5d13739ce")
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Unlock", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{purchase.Id}");
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

            var purchase = new PurchaseRequest(
                Guid.Parse("0c5a7011-2401-42c2-bd8a-c0b5d13739ce")
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Unlock", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{purchase.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.Authorizing, result?.Data?.Status);
        }

        [Fact]
        public async Task UnlockPurchaseWithOutSuccessAsClientn()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "4922766E-D3BA-4D4C-99B0-093D5977D41F");
            client.DefaultRequestHeaders.Add("role", "client");

            var purchase = new PurchaseRequest(
                Guid.Parse("0c5a7011-2401-42c2-bd8a-c0b5d13739ce")
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Unlock", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(MaterialPurchaseErrors.AuthorizationInvalid), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task ConfirmDeliveryPurchaseWithSuccessn()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("role", "client");

            var dateExpected = DateTime.UtcNow.AddDays(1);

            var purchase = new ConfirmDeliveryDateRequest(
                Guid.Parse("3887e6ff-13a4-4665-a8e3-14632d7dd2ce"),
                dateExpected
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Delivery/Confirm", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{content?.Data?.Id}");
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

            var dateExpected = DateTime.UtcNow.AddDays(1);

            var purchase = new ConfirmDeliveryDateRequest(
                Guid.Parse("3887e6ff-13a4-4665-a8e3-14632d7dd2ce"),
                dateExpected
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Delivery/Confirm", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(MaterialPurchaseErrors.AuthorizationInvalid), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task ConfirmDeliveryPurchaseThatHasDeliveryProblemWithSuccessn()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("role", "client");

            var dateExpected = DateTime.UtcNow.AddDays(1);

            var purchase = new ConfirmDeliveryDateRequest(
                Guid.Parse("3887e6ff-13a4-4665-a8e3-14632d7dd2ce"),
                dateExpected
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Purchase/Delivery/Confirm", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{content?.Data?.Id}");
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

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("role", "client");

            var dateExpected = DateTime.UtcNow.AddDays(1);



            var purchase = new ReceiveDeliveryRequest(
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
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{content?.Data?.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.DeliveryProblem, result?.Data?.Status);
        }

        [Fact]
        public async Task DeliveryAllWithSuccess()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("role", "client");

            var dateExpected = DateTime.UtcNow.AddDays(1);



            var purchase = new ReceiveDeliveryRequest(
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
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{content?.Data?.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.Closed, result?.Data?.Status);
        }

        [Fact]
        public async Task DeliveryWithExcessMaterial()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("role", "client");

            var dateExpected = DateTime.UtcNow.AddDays(1);



            var purchase = new ReceiveDeliveryRequest(
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

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("role", "client");

            var dateExpected = DateTime.UtcNow.AddDays(1);



            var purchase = new ReceiveDeliveryRequest(
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
    }
}
