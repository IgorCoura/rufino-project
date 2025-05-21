using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.DocumentCommands.CreateDocument;
using PeopleManagement.Application.Commands.DocumentCommands.GenerateDocumentToSign;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocument;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentSigned;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentToSign;
using PeopleManagement.Application.Commands.DocumentCommands.UpdateDocumentUnitDetails;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.IntegrationTests.Configs;
using PeopleManagement.IntegrationTests.Data;
using System.ComponentModel.Design;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text.Json.Nodes;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PeopleManagement.IntegrationTests.Tests
{
    public class DocumentTests(PeopleManagementWebApplicationFactory factory) : IClassFixture<PeopleManagementWebApplicationFactory>
    {
        private readonly PeopleManagementWebApplicationFactory _factory = factory;
        [Fact]
        public async Task CreateDocumentWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var document = await context.InsertDocument(cancellationToken);
            var documentUnit = await document.InsertOneDocumentInDocument();
            await context.SaveChangesAsync(cancellationToken);

            var date = DateOnly.FromDateTime(DateTime.UtcNow);

            var command = new UpdateDocumentUnitDetailsModel(
                    documentUnit.Id,
                    document.Id,
                    document.EmployeeId,
                    date
                );


            client.InputHeaders([document.CompanyId]);
            var response = await client.PutAsJsonAsync($"/api/v1/{document.CompanyId}/document/documentunit", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(CreateDocumentResponse)) as CreateDocumentResponse ?? throw new ArgumentNullException();
            var result = await context.Documents.AsNoTracking().Include(x => x.DocumentsUnits.Where(x => x.Id == content.Id)).FirstOrDefaultAsync(x => x.Id == document.Id) ?? throw new ArgumentNullException();
            var documentResult = result.DocumentsUnits.First();
            Assert.Equal(date.Day, documentResult.Date.Day);
        }

        [Fact]
        public async Task CreateDocumentWithTimeConflict()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var company = await context.InsertCompany(cancellationToken);
            var role = await context.InsertRole(company.Id, cancellationToken);
            var documentTemplate = await context.InsertDocumentTemplate(company.Id, cancellationToken);
            var documentTemplate2 = await context.InsertDocumentTemplate(company.Id, cancellationToken);
            var requiresDocuments = await context.InsertRequireDocuments(company.Id, role.Id, [documentTemplate.Id, documentTemplate2.Id], cancellationToken);

            var emplyeeActive = await context.InsertEmployeeActive(company.Id, role.Id, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var documentWithConflict = await context.InsertDocument(emplyeeActive, documentTemplate, requiresDocuments, cancellationToken);
            var documentUnitWithConflict = await documentWithConflict.InsertOneDocumentWithInfoInDocument();
            await context.SaveChangesAsync(cancellationToken);

            var document = await context.InsertDocument(emplyeeActive, documentTemplate2, requiresDocuments, cancellationToken);
            var documentUnit = await document.InsertOneDocumentInDocument();
            await context.SaveChangesAsync(cancellationToken);

            var command = new UpdateDocumentUnitDetailsModel(
                    documentUnit.Id,
                    document.Id,
                    document.EmployeeId,
                    documentUnitWithConflict.Date
                );


            client.InputHeaders([company.Id]);
            var response = await client.PutAsJsonAsync($"/api/v1/{document.CompanyId}/document/documentunit", command);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<JsonNode>();
            var erroCode = content!["errors"]!["DocumentService"]![0]!["Code"]!;
            Assert.Equal("PMD.DOC10", erroCode.ToString());
        }

        [Fact]
        public async Task GeneratePdfWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();
            
            var document = await context.InsertDocument(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            var documentUnit = await document.InsertOneDocumentWithInfoInDocument();
            await context.SaveChangesAsync(cancellationToken);

            client.InputHeaders([document.CompanyId]);
            var response = await client.GetAsync($"/api/v1/{document.CompanyId}/document/generate/{document.EmployeeId}/{document.Id}/{documentUnit.Id}");         

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsByteArrayAsync();
            Assert.NotNull(content);
            Assert.True(content.Length > 130000 && content.Length < 153000);

            //Debug
            //var directory = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            //var path = Path.Combine(directory, $"{Guid.NewGuid()}.pdf");
            //if(!Directory.Exists(directory))
            //    Directory.CreateDirectory(directory);
            //File.WriteAllBytes(path, content);
        }

        [Fact]
        public async Task InsertPdfWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var document = await context.InsertDocument(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            var documentUnit = await document.InsertOneDocumentWithInfoInDocument();
            await context.SaveChangesAsync(cancellationToken);


            using var content = new MultipartFormDataContent();

            // Carregue o arquivo PDF em um stream
            var path = Path.Combine("DataForTests", "199f760b-601d-4a05-aee4-d0a9dbcc6b4d.pdf");
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var streamContent = new StreamContent(fileStream);
                
            // Adicione o conteúdo do tipo arquivo ao multipart/form-data
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(streamContent, "formFile", Path.GetFileName(path));

            content.Add(new StringContent(documentUnit.Id.ToString()), "documentUnitId");
            content.Add(new StringContent(document.Id.ToString()), "documentId");
            content.Add(new StringContent(document.EmployeeId.ToString()), "employeeId");

            client.InputHeaders([document.CompanyId]);
            var response = await client.PostAsync($"/api/v1/{document.CompanyId}/document/insert", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contentResponse = await response.Content.ReadFromJsonAsync<InsertDocumentToSignResponse>() ?? throw new ArgumentNullException();
            var result = await context.Documents.AsNoTracking().Include(x => x.DocumentsUnits.Where(x => x.Id == contentResponse.Id)).FirstOrDefaultAsync(x => x.Id == document.Id) ?? throw new ArgumentNullException();
            var documentResponse = result.DocumentsUnits.First();
            Assert.Equal(Extension.PDF, documentResponse.Extension);
            Assert.Equal(typeof(Guid), documentResponse.Id.GetType());

            using var scope = _factory.Services.CreateScope();

            var blobService = scope.ServiceProvider.GetRequiredService<IBlobService>();

            var stream = await blobService.DownloadAsync(documentResponse.GetNameWithExtension, document.CompanyId.ToString(), cancellationToken);

            Assert.NotNull(stream);
            Assert.True(stream.Length > 0);
            
        }


        [Fact(Skip = "Utilizar uma API externa para ser testando")]
        public async Task GenerateDocumentToSignWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var document = await context.InsertDocument(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            var documentUnit = await document.InsertOneDocumentWithInfoInDocument();
            await context.SaveChangesAsync(cancellationToken);

            var command = new GenerateDocumentToSignModel(documentUnit.Id, document.Id, document.EmployeeId, DateTime.UtcNow.AddDays(30), 1);

            client.InputHeaders([document.CompanyId]);
            var response = await client.PostAsJsonAsync($"/api/v1/{document.CompanyId}/document/generate/send2sign", command);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync(typeof(GenerateDocumentToSignResponse)) as GenerateDocumentToSignResponse ?? throw new ArgumentNullException();
            var result = await context.Documents.AsNoTracking().Include(x => x.DocumentsUnits.Where(x => x.Id == content.Id)).FirstOrDefaultAsync(x => x.Id == document.Id) ?? throw new ArgumentNullException();
            var documentResult = result.DocumentsUnits.First();
            Assert.Equal(DocumentStatus.AwaitingSignature, result.Status);
            Assert.Equal(DocumentUnitStatus.AwaitingSignature, documentResult.Status);
        }


        [Fact(Skip = "Utilizar uma API externa para ser testando")]
        public async Task InsertDocumentToSignWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var document = await context.InsertDocument(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            var documentUnit = await document.InsertOneDocumentWithInfoInDocument();
            await context.SaveChangesAsync(cancellationToken);

            using var content = new MultipartFormDataContent();

            // Carregue o arquivo PDF em um stream
            var path = Path.Combine("DataForTests", "199f760b-601d-4a05-aee4-d0a9dbcc6b4d.pdf");
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var streamContent = new StreamContent(fileStream);

            // Adicione o conteúdo do tipo arquivo ao multipart/form-data
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(streamContent, "formFile", Path.GetFileName(path));

            content.Add(new StringContent(documentUnit.Id.ToString()), "DocumentUnitId");
            content.Add(new StringContent(document.Id.ToString()), "documentId");
            content.Add(new StringContent(document.EmployeeId.ToString()), "employeeId");
            content.Add(new StringContent(DateTime.UtcNow.AddDays(30).ToString()), "DateLimitToSign");
            content.Add(new StringContent("1"), "EminderEveryNDays");

            client.InputHeaders([document.CompanyId]);
            var response = await client.PostAsync($"/api/v1/{document.CompanyId}/document/insert/send2sign", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contentResponse = await response.Content.ReadFromJsonAsync(typeof(InsertDocumentResponse)) as InsertDocumentResponse ?? throw new ArgumentNullException();
            var result = await context.Documents.AsNoTracking().Include(x => x.DocumentsUnits.Where(x => x.Id == contentResponse.Id)).FirstOrDefaultAsync(x => x.Id == document.Id) ?? throw new ArgumentNullException();
            var documentResult = result.DocumentsUnits.First();
            Assert.Equal(DocumentStatus.AwaitingSignature, result.Status);
            Assert.Equal(DocumentUnitStatus.AwaitingSignature, documentResult.Status);

        }

        [Fact]
        public async Task InsertDocSignerWithSuccess()
        {
            var cancellationToken = new CancellationToken();

            var context = _factory.GetContext();
            var client = _factory.CreateClient();

            var document = await context.InsertDocument(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            var documentUnit = await document.InsertOneDocumentWithInfoInDocument();
            await context.SaveChangesAsync(cancellationToken);

            var commandString = @"
                {
                    ""event_type"": ""doc_signed"",
                    ""sandbox"": false,
                    ""external_id"": ""id-suaaplicacao-e32213ds-243"",
                    ""open_id"": 23,
                    ""token"": ""cce11abf-abcd-abcd-a657-25b55f185f16"",
                    ""name"": ""sample.pdf"",
                    ""folder_path"": ""/"",
                    ""status"": ""signed"",
                    ""lang"": ""pt-br"",
                    ""original_file"": ""https://zapsign.s3.amazonaws.com/2022/1/pdf/63d19807-cbfa-4b51-8571-215ad0f4eb98/ca42e7be-c932-482c-b70b-92ad7aea04be.pdf"",
                    ""signed_file"": ""https://zapsign.s3.amazonaws.com/2022/1/pdf/63d19807-cbfa-4b51-8571-215ad0f4eb98/ca42e7be-c932-482c-b70b-92ad7aea04be.pdf"",
                    ""created_through"": ""web"",
                    ""deleted"": false,
                    ""deleted_at"": null,
                    ""signed_file_only_finished"": false,
                    ""disable_signer_emails"": false,
                    ""brand_logo"": """",
                    ""brand_primary_color"": """",
                    ""created_at"": ""2021-06-07T19:21:59.609067Z"",
                    ""last_update_at"": ""2021-06-07T19:22:21.838310Z"",
                    ""signers"": [
                        {
                            ""external_id"": """",
                            ""token"": ""b14f141f-abcd-abcd-8dfa-0bdac0ec806a"",
                            ""status"": ""signed"",
                            ""name"": ""Fulano Silva"",
                            ""lock_name"": false,
                            ""email"": ""ola@zapsign.com.br"",
                            ""lock_email"": false,
                            ""phone_country"": ""55"",
                            ""phone_number"": ""1155551111"",
                            ""lock_phone"": false,
                            ""times_viewed"": 1,
                            ""last_view_at"": ""2021-06-07T19:22:12.875967Z"",
                            ""signed_at"": ""2021-06-07T19:22:19.956056Z"",
                            ""auth_mode"": ""assinaturaTela"",
                            ""qualification"": """",
                            ""require_selfie_photo"": false,
                            ""require_document_photo"": false,
                            ""geo_latitude"": ""-23.559298"",
                            ""geo_longitude"": ""-46.683343"",
                            ""resend_attempts"": {
                                ""whatsapp"": 0,
                                ""email"": 0,
                                ""sms"": 0
                            },
                            ""cpf"": ""99999999999"",
                            ""cnpj"": ""99999999999""
                        }
                    ],
                    ""answers"": [
                        {
                            ""variable"": ""NOME COMPLETO"",
                            ""value"": ""Nome Teste""
                        },
                        {
                            ""variable"": ""NÚMERO DO CPF"",
                            ""value"": ""99999999999""
                        },
                        {
                            ""variable"": ""ENDEREÇO COMPLETO"",
                            ""value"": ""Endereço teste""
                        }
                    ],
                    ""signer_who_signed"": {
                        ""external_id"": """",
                        ""token"": ""b14f141f-abcd-abcd-8dfa-0bdac0ec806a"",
                        ""status"": ""signed"",
                        ""name"": ""Fulano Silva"",
                        ""lock_name"": false,
                        ""email"": ""ola@zapsign.com.br"",
                        ""lock_email"": false,
                        ""phone_country"": ""55"",
                        ""phone_number"": ""1155551111"",
                        ""lock_phone"": false,
                        ""times_viewed"": 1,
                        ""last_view_at"": ""2021-06-07T19:22:12.875967Z"",
                        ""signed_at"": ""2021-06-07T19:22:19.956056Z"",
                        ""auth_mode"": ""assinaturaTela"",
                        ""qualification"": """",
                        ""require_selfie_photo"": false,
                        ""require_document_photo"": false,
                        ""geo_latitude"": ""-23.559298"",
                        ""geo_longitude"": ""-46.683343""
                    }
                }";

            JsonNode jsonNode = JsonNode.Parse(commandString)!;

            jsonNode["external_id"] = documentUnit.Id;
            var command = new StringContent(jsonNode.ToString());
            command.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            client.InputHeaders([document.CompanyId]);
            var response = await client.PostAsync($"/api/v1/{document.CompanyId}/document/insert/signer", command);

            var response231 = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contentResponse = await response.Content.ReadFromJsonAsync<InsertDocumentSignedResponse>() ?? throw new ArgumentNullException();
            var result = await context.Documents.AsNoTracking().Include(x => x.DocumentsUnits.Where(x => x.Id == contentResponse.Id)).FirstOrDefaultAsync(x => x.Id == document.Id) ?? throw new ArgumentNullException();
            var documentResponse = result.DocumentsUnits.First();
            Assert.Equal(Extension.PDF, documentResponse.Extension);
            Assert.Equal(typeof(Guid), documentResponse.Id.GetType());

            using var scope = _factory.Services.CreateScope();

            var blobService = scope.ServiceProvider.GetRequiredService<IBlobService>();

            var stream = await blobService.DownloadAsync(documentResponse.GetNameWithExtension, document.CompanyId.ToString(), cancellationToken);

            Assert.NotNull(stream);
            Assert.True(stream.Length > 0);
        }



    }
}
