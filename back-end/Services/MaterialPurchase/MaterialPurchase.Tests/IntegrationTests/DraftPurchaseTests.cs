using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using Commom.Tests.Models;
using MaterialPurchase.Domain.Models.Request;
using MaterialPurchase.Domain.Models.Response;
using MaterialPurchase.Tests.Utils;
using System.Net;

namespace MaterialPurchase.Tests.IntegrationTests
{
    public class DraftPurchaseTests
    {
        

        [Fact]
        public async Task CreatePurchaseWithSuccess()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("Role", "client");

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
            var response = await client.PostAsJsonAsync("/api/v1/DraftPurchase/Create", newPurchase);

            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync(typeof(BaseResponse<PurchaseResponse>)).Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{content.Data.Id}");
            Assert.True(newPurchase.EqualExtesion(result!.Data!));
        }

        [Fact]
        public async Task CreatePurchaseWithExistProvider()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("Role", "client");

            var newPurchase = new CreateDraftPurchaseRequest(
                Guid.Parse("6bb5a348-64d3-4c92-aedd-63319de238c4"),
                Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
                99,
                new CreateMaterialDraftPurchaseRequest[]
                {
                    new CreateMaterialDraftPurchaseRequest(Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"), Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"), 77, 33),
                    new CreateMaterialDraftPurchaseRequest(Guid.Parse("91909CEA-E52C-4945-AAA9-1E50266C1C66"), Guid.Parse("2C377F5B-DA7A-4A2E-87BB-1C16894ADC0D"), 77, 33),
                });

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/DraftPurchase/Create", newPurchase);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.ReferenceConstraintViolation), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task CreatePurchaseWithInvalidProperty()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("Role", "client");

            var newPurchase = new CreateDraftPurchaseRequest(
                Guid.Parse("6bb5a348-64d3-4c92-aedd-63319de238c4"),
                Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
                -9,
                new CreateMaterialDraftPurchaseRequest[]
                {
                    new CreateMaterialDraftPurchaseRequest(Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"), Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"), -77, -33),
                    new CreateMaterialDraftPurchaseRequest(Guid.Parse("91909CEA-E52C-4945-AAA9-1E50266C1C66"), Guid.Parse("2C377F5B-DA7A-4A2E-87BB-1C16894ADC0D"), -77, -33),
                });

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/DraftPurchase/Create", newPurchase);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.All(content?.Errors, x => Assert.Equal(RecoverError.GetCode(CommomErrors.GreaterThanOrEqualValidator), x.ErrorCode));
        }


        [Fact]
        public async Task UpdatePurchaseWithExistProvider()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("Role", "client");

            var purchase = new DraftPurchaseRequest(
                Guid.Parse("CA100B9F-8D13-4E64-ADBC-A90462D05A9A"),
                Guid.Parse("6bb5a348-64d3-4c92-aedd-63319de238c4"),
                Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
                99,
                new MaterialDraftPurchaseRequest[]
                {
                    new MaterialDraftPurchaseRequest(Guid.Parse("005D1FEF-3308-4B27-8CB2-2CE610C1E231"), Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"), Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"), 97, 22)
                });

           
            //Act
            var response = await client.PostAsJsonAsync("/api/v1/DraftPurchase/Update", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.ReferenceConstraintViolation), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task UpdatePurchaseWithInvalidProperty()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("Role", "client");

            var purchase = new DraftPurchaseRequest(
                Guid.Parse("CA100B9F-8D13-4E64-ADBC-A90462D05A9A"),
                Guid.Parse("8299C0DC-927D-45DE-B2C8-71C38FAF9384"),
                Guid.Parse("651E60AD-DDAC-45F8-B2ED-60D2DB924AE7"),
                -99,
                new MaterialDraftPurchaseRequest[]
                {
                    new MaterialDraftPurchaseRequest(Guid.Parse("005D1FEF-3308-4B27-8CB2-2CE610C1E231"), Guid.Parse("54D98347-4009-466C-8A6E-AC01EC3F9A7C"), Guid.Parse("9894CE53-89E3-47AE-BEDE-7D1AEC6F98F0"), -97, -22)
                });


            //Act
            var response = await client.PostAsJsonAsync("/api/v1/DraftPurchase/Update", purchase);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.All(content?.Errors, x => Assert.Equal(RecoverError.GetCode(CommomErrors.GreaterThanOrEqualValidator), x.ErrorCode));
        }

        [Fact]
        public async Task UpdatePurchaseWithSuccess()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("Role", "client");

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
            var response = await client.PostAsJsonAsync("/api/v1/DraftPurchase/Update", purchase);


            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync(typeof(BaseResponse<PurchaseResponse>)).Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{content!.Data!.Id}");
            Assert.True(purchase.EqualExtesion(result!.Data!));
        }


        [Fact]
        public async Task DeletePurchaseWithSuccess()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("Role", "client");

            var purchase = new PurchaseRequest(
                Guid.Parse("CA100B9F-8D13-4E64-ADBC-A90462D05A9A")
                );

            //Act
            await client.PostAsJsonAsync("/api/v1/DraftPurchase/Delete", purchase);

            //Asssert 
            var result = await client.GetAsync($"/api/v1/RecoverPurchase/Complete/{purchase.Id}");
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            var errorResponse = result.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.PropertyNotFound), errorResponse?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task SendPurchaseWithSuccess()
        {
            //Arrange
            var factory = new MaterialPurchaseFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "FDEC4D71-4300-4F5D-8146-9C3E8D62528B");
            client.DefaultRequestHeaders.Add("Role", "client");

            var purchase = new PurchaseRequest(
                Guid.Parse("CA100B9F-8D13-4E64-ADBC-A90462D05A9A")
                );

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/DraftPurchase/Send", purchase);

            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<BaseResponse<PurchaseResponse>>().Result as BaseResponse<PurchaseResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<CompletePurchaseResponse>>($"/api/v1/RecoverPurchase/Complete/{purchase.Id}");
            Assert.True(content?.Success);
            Assert.Equal(Domain.Enum.PurchaseStatus.Authorizing, result?.Data?.Status);
        }


    }
}
