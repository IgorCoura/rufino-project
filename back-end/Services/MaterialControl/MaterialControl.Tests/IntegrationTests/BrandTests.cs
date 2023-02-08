using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using Commom.Tests.Models;
using MaterialControl.Domain.Models.Request;
using MaterialControl.Domain.Models.Response;
using MaterialControl.Tests.Utils;
using System.Net;

namespace MaterialControl.Tests.IntegrationTests
{
    public class BrandTests
    {
        [Fact]
        public async Task CreateBrandWithSuccess()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new CreateBrandRequest("Tigre", "Produto de cano");

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Brand", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync(typeof(BaseResponse<BrandResponse>)).Result as BaseResponse<BrandResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<BrandResponse>>($"/api/v1/Brand/{content.Data.Id}");
            Assert.True(request.EqualExtesion(result!.Data!));
        }

        [Fact]
        public async Task CreateBrandWithInvalidProperty()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new CreateBrandRequest(string.Create(51, "A", (buffer, value) => buffer.Fill(value[0])), "");

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Brand", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.MaximumLengthValidator), content?.Errors[0].ErrorCode);
            Assert.Equal(RecoverError.GetCode(CommomErrors.NotEmptyValidator), content?.Errors[1].ErrorCode);
        }

        [Fact]
        public async Task UpdateBrandWithSuccess()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new BrandRequest(Guid.Parse("6e59a809-88c7-4e75-a684-4e5b0948ab20"), "Tigre", "Produto de cano");

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/Brand", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync(typeof(BaseResponse<BrandResponse>)).Result as BaseResponse<BrandResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<BrandResponse>>($"/api/v1/Brand/{content.Data.Id}");
            Assert.True(request.EqualExtesion(result!.Data!));
        }

        [Fact]
        public async Task UpdateBrandWithInvalidProperty()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new BrandRequest(Guid.Parse("6e59a809-88c7-4e75-a684-4e5b0948ab20"), string.Create(51, "A", (buffer, value) => buffer.Fill(value[0])), "");

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/Brand", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.MaximumLengthValidator), content?.Errors[0].ErrorCode);
            Assert.Equal(RecoverError.GetCode(CommomErrors.NotEmptyValidator), content?.Errors[1].ErrorCode);
        }

        [Fact]
        public async Task UpdateBrandNonexistent()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new BrandRequest(Guid.Parse("3d59a809-88c7-4e75-a684-4e5b0948ab20"), "Tigre", "Produto de cano");

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/Brand", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.PropertyNotFound), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task DeleteBrandWithSuccess()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            //Act
            var response = await client.DeleteAsync("/api/v1/Brand/6e59a809-88c7-4e75-a684-4e5b0948ab20");

            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);          
            var result = await client.GetAsync("/api/v1/Brand/6e59a809-88c7-4e75-a684-4e5b0948ab20");
            var error = result.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.PropertyNotFound), error.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task DeleteBrandNonexistent()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            //Act
            var response = await client.DeleteAsync("/api/v1/Brand/5e59a809-88c7-4e75-a684-4e5b0948ab20");

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.PropertyNotFound), content?.Errors[0].ErrorCode);
        }
    }
}
