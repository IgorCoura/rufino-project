using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using Commom.Tests.Models;
using MaterialControl.Domain.Models.Request;
using MaterialControl.Domain.Models.Response;
using MaterialControl.Tests.Utils;
using System.Net;

namespace MaterialControl.Tests.IntegrationTests
{
    public class UnityTests
    {
        [Fact]
        public async Task CreateUnityWithSuccess()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new CreateUnityRequest("Peça");

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Unity", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync(typeof(BaseResponse<UnityResponse>)).Result as BaseResponse<UnityResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<UnityResponse>>($"/api/v1/Unity/{content.Data.Id}");
            Assert.True(request.EqualExtesion(result!.Data!));
        }

        [Fact]
        public async Task CreateUnityWithInvalidProperty()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new CreateUnityRequest(string.Create(30, "A", (buffer, value) => buffer.Fill(value[0])));

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Unity", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.MaximumLengthValidator), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task CreateUnityWithDuplicateUnity()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new CreateUnityRequest("Metro");

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Unity", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.UniqueConstraintViolation), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task UpdateUnityWithSuccess()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new UnityRequest(Guid.Parse("ec9ec3fd-b6b8-43e1-80b3-60b7a22152c1"), "Peças");

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/Unity", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync(typeof(BaseResponse<UnityResponse>)).Result as BaseResponse<UnityResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<UnityResponse>>($"/api/v1/Unity/{content.Data.Id}");
            Assert.True(request.EqualExtesion(result!.Data!));
        }

        [Fact]
        public async Task UpdateUnityWithInvalidProperty()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new UnityRequest(Guid.Parse("6e59a809-88c7-4e75-a684-4e5b0948ab20"), "");

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/Unity", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.NotEmptyValidator), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task UpdateBrandNonexistent()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new UnityRequest(Guid.Parse("3d59a809-88c7-4e75-a684-4e5b0948ab20"), "METRO");

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/Unity", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.PropertyNotFound), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task UpdateUnityWithDuplicateUnity()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new UnityRequest(Guid.Parse("f6a65b2e-d765-4e73-a458-cf6e9cc375ef"),"Metro");

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/Unity", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.UniqueConstraintViolation), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task DeleteUnityWithSuccess()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            //Act
            var response = await client.DeleteAsync("/api/v1/Unity/f6a65b2e-d765-4e73-a458-cf6e9cc375ef");

            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await client.GetAsync("/api/v1/Unity/f6a65b2e-d765-4e73-a458-cf6e9cc375ef");
            var error = result.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.PropertyNotFound), error.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task DeleteUnityNonexistent()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            //Act
            var response = await client.DeleteAsync("/api/v1/Unity/71b36eec-2db3-4cb1-8c98-5c894f7cc264");

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.PropertyNotFound), content?.Errors[0].ErrorCode);
        }
    }
}
