using Commom.Domain.Errors;
using Commom.Domain.Exceptions;
using Commom.Tests.Models;
using MaterialControl.Domain.Models.Request;
using MaterialControl.Domain.Models.Response;
using MaterialControl.Tests.Utils;
using System.Net;

namespace MaterialControl.Tests.IntegrationTests
{
    public class MaterialTests
    {
        [Fact]
        public async Task CreateMaterialWithSuccess()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new CreateMaterialRequest("Material", "Material", Guid.Parse("ec9ec3fd-b6b8-43e1-80b3-60b7a22152c1"));

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Material", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync(typeof(BaseResponse<MaterialResponse>)).Result as BaseResponse<MaterialResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<MaterialResponse>>($"/api/v1/Material/{content.Data.Id}");
            Assert.True(request.EqualExtesion(result!.Data!));
        }

        [Fact]
        public async Task CreateMaterialWithInvalidProperty()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new CreateMaterialRequest(string.Create(101, "A", (buffer, value) => buffer.Fill(value[0])), "", Guid.Parse("ec9ec3fd-b6b8-43e1-80b3-60b7a22152c1"));

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Material", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.MaximumLengthValidator), content?.Errors[0].ErrorCode);
            Assert.Equal(RecoverError.GetCode(CommomErrors.NotEmptyValidator), content?.Errors[1].ErrorCode);
        }

        [Fact]
        public async Task CreateMaterialWithDuplicateMaterial()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new CreateMaterialRequest("Material1", "Material01", Guid.Parse("ec9ec3fd-b6b8-43e1-80b3-60b7a22152c1"));

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Material", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.UniqueConstraintViolation), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task CreateMaterialWithInvalidUnity()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new CreateMaterialRequest("Material", "Material", Guid.Parse("ac9ec3fd-b6b8-43e1-80b3-60b7a22152c1"));

            //Act
            var response = await client.PostAsJsonAsync("/api/v1/Material", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.ReferenceConstraintViolation), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task UpdateMaterialWithSuccess()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new MaterialRequest(Guid.Parse("f77667aa-45d6-43dd-b929-af132e879415"),"Material", "Material", Guid.Parse("ec9ec3fd-b6b8-43e1-80b3-60b7a22152c1"));

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/Material", request);
            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync(typeof(BaseResponse<MaterialResponse>)).Result as BaseResponse<MaterialResponse>;
            var result = await client.GetFromJsonAsync<BaseResponse<MaterialResponse>>($"/api/v1/Material/{content.Data.Id}");
            Assert.True(request.EqualExtesion(result!.Data!));
        }

        [Fact]
        public async Task UpdateMaterialWithInvalidProperty()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new MaterialRequest(Guid.Parse("f77667aa-45d6-43dd-b929-af132e879415"), string.Create(101, "A", (buffer, value) => buffer.Fill(value[0])), "", Guid.Parse("ec9ec3fd-b6b8-43e1-80b3-60b7a22152c1"));

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/Material", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.MaximumLengthValidator), content?.Errors[0].ErrorCode);
            Assert.Equal(RecoverError.GetCode(CommomErrors.NotEmptyValidator), content?.Errors[1].ErrorCode);
        }

        [Fact]
        public async Task UpdateMaterialNonexistent()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new MaterialRequest(Guid.Parse("a77667aa-45d6-43dd-b929-af132e879415"), "Material", "Material", Guid.Parse("ec9ec3fd-b6b8-43e1-80b3-60b7a22152c1"));

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/Material", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.PropertyNotFound), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task UpdateMaterialWithDuplicateMaterial()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new MaterialRequest(Guid.Parse("f77667aa-45d6-43dd-b929-af132e879415"), "Material2", "Material", Guid.Parse("ec9ec3fd-b6b8-43e1-80b3-60b7a22152c1"));

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/Material", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.UniqueConstraintViolation), content?.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task UpdateMaterialWithInvalidUnity()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            var request = new MaterialRequest(Guid.Parse("f77667aa-45d6-43dd-b929-af132e879415"), "Material", "Material", Guid.Parse("ec9ec3fd-b6b8-43e1-80b3-60b7a22152c5"));

            //Act
            var response = await client.PutAsJsonAsync("/api/v1/Material", request);

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.ReferenceConstraintViolation), content?.Errors[0].ErrorCode);
        }


        [Fact]
        public async Task DeleteMaterialWithSuccess()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            //Act
            var response = await client.DeleteAsync("/api/v1/Material/f77667aa-45d6-43dd-b929-af132e879415");

            //Asssert 
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await client.GetAsync("/api/v1/Material/f77667aa-45d6-43dd-b929-af132e879415");
            var error = result.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.PropertyNotFound), error.Errors[0].ErrorCode);
        }

        [Fact]
        public async Task DeleteMaterialNonexistent()
        {
            //Arrange
            var factory = new MaterialControlFactory();
            var client = factory.CreateClient();

            client.DefaultRequestHeaders.Add("Sid", "71b36eec-2db3-4cb1-8c98-5c894f7cc264");
            client.DefaultRequestHeaders.Add("Role", "client");

            //Act
            var response = await client.DeleteAsync("/api/v1/Material/71b36eec-2db3-4cb1-8c98-5c894f7cc264");

            //Asssert 
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = response.Content.ReadFromJsonAsync<ErrorResponse>().Result as ErrorResponse;
            Assert.Equal(RecoverError.GetCode(CommomErrors.PropertyNotFound), content?.Errors[0].ErrorCode);
        }
    }
}
