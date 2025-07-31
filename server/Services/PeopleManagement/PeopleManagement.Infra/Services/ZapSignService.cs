using Azure.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Options;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.WebHookAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.SeedWord;
using PeopleManagement.Infra.Context;
using PeopleManagement.Infra.Repository;
using PuppeteerSharp;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PeopleManagement.Infra.Services
{
    public class ZapSignService(HttpClient httpClient, ILogger<ZapSignService> logger , IAuthorizationService authorizationService, SignOptions signOptions, IWebHookRepository webHookRepository) : ISignService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<ZapSignService> _logger = logger;
        private readonly IAuthorizationService _authorizationService = authorizationService;
        private readonly SignOptions _signOptions = signOptions;
        private readonly IWebHookRepository _webHookRepository = webHookRepository;

        public async Task SendToSignatureWithWhatsapp(Stream documentStream, Guid documentUnitId, Document document, Company company, 
            Employee employee, PlaceSignature[] placeSignatures, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default)
        {
            var signerOptions = new SignerOptions(SignatureType.DigitalSignatureAndWhatsapp, false, true, true);
            await SendToSignature(signerOptions, documentStream, documentUnitId, document, company, employee, placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);
        }
        public async Task SendToSignatureWithSelfie(Stream documentStream, Guid documentUnitId, Document document, Company company,
            Employee employee, PlaceSignature[] placeSignatures, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default)
        {
            var signerOptions = new SignerOptions(SignatureType.DigitalSignature, true, true, true);
            await SendToSignature(signerOptions, documentStream, documentUnitId, document, company, employee, placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);
        }
        private async Task SendToSignature(SignerOptions signerOptions, Stream documentStream, Guid documentUnitId, Document document, 
            Company company, Employee employee, PlaceSignature[] placeSignatures, DateTime dateLimitToSign, int eminderEveryNDays, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Sending document to ZapSign for signature. DocumentUnitId: {DocumentUnitId}, EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                documentUnitId, employee.Id, company.Id);
            var documentBase64 = ConvertStreamToBase64(documentStream);

            var corporateName = company.CorporateName.Value.Replace("/", "");

            var folderPath = $"{corporateName}/{employee.Name}";


            

            var signerArray = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = $"{employee.Name}",
                    ["auth_mode"] = $"{signerOptions.AuthMode.JsonName}",
                    ["email"] = $"{employee.Contact!.Email}",
                    ["send_automatic_email"] = true,
                    ["send_automatic_whatsapp"] = true,
                    ["send_automatic_whatsapp_signed_file"] = true,
                    ["phone_country"] = $"55",
                    ["phone_number"] = $"{employee.Contact!.CellPhone}",
                    ["lock_email"] = signerOptions.LockEmail,
                    ["blank_email"] = true,
                    ["lock_phone"] = signerOptions.LockEmail,
                    ["blank_phone"] = true,
                    ["lock_name"] = true,
                    ["cpf"] = $"{employee.IdCard!.Cpf}",
                    ["external_id"] = $"{employee.Id}"
                }
            };

            var documentToSign = new JsonObject
            {
                ["name"] = $"{document.Name} - {DateOnly.FromDateTime(DateTime.UtcNow)}",
                ["external_id"] = $"{documentUnitId}",
                ["lang"] = "pt-br",
                ["brand_name"] = $"{company.FantasyName}",
                ["folder_path"] = $"{folderPath}",
                ["date_limit_to_sign"] = dateLimitToSign,
                ["reminder_every_n_days"] = eminderEveryNDays,
                ["signers"] = signerArray,
                ["base64_pdf"] = documentBase64
            };

            const int maxRetries = 6;
            int retryCount = 0;
            HttpResponseMessage response;

            do
            {
                response = await _httpClient.PostAsync("/api/v1/docs/", new StringContent(documentToSign.ToString()), cancellationToken);
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken); // Exponential backoff  
            }
            while (response.IsSuccessStatusCode == false && retryCount < maxRetries);

            

            if (response.IsSuccessStatusCode == false)
            {
                var debug = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send document to ZapSign for signature. DocumentUnitId: {DocumentUnitId}, EmployeeId: {EmployeeId}, CompanyId: {CompanyId}, Response: {Response}",
                    documentUnitId, employee.Id, company.Id, debug);
                throw new DomainException(this, InfraErrors.SignDoc.ErrorSendDocToSign(documentUnitId));
            }
                

            var content = await response.Content.ReadFromJsonAsync<JsonNode>();
            var docToken = content?["token"]?.ToString() ?? "";
            var signerToken = content?["signers"]?[0]?["token"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(docToken) || string.IsNullOrEmpty(signerToken))
                throw new DomainException(this, InfraErrors.SignDoc.ErrorSendDocToSign(documentUnitId));


            if (placeSignatures.Length > 0)
            {
                var resultPlaceSignature = await PlaceSignature(docToken, signerToken, placeSignatures, cancellationToken);

                if (resultPlaceSignature == false)
                {
                    _logger.LogError("Failed to place signatures on document. DocumentUnitId: {DocumentUnitId}, EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                        documentUnitId, employee.Id, company.Id);
                    await DeleteDocument(docToken, cancellationToken);
                    throw new DomainException(this, InfraErrors.SignDoc.ErrorSendDocToSign(documentUnitId));
                }

            }

            _logger.LogInformation("Document sent to ZapSign for signature successfully. DocumentUnitId: {DocumentUnitId}, EmployeeId: {EmployeeId}, CompanyId: {CompanyId}", documentUnitId, employee.Id, company.Id);
        }

        public async Task<DocSignedModel?> GetFileFromDocSignedEvent(JsonNode contentBody, CancellationToken cancellationToken = default)
        {
            var status = contentBody?["status"]?.ToString() ?? "";
            var allSigned = string.Equals(status, "signed");

            if (allSigned == false)
                return null;

            var documentUrl = contentBody?["signed_file"]?.ToString() ?? "";
            var documentUnitId = contentBody?["external_id"]?.ToString() ?? "";
            _httpClient.DefaultRequestHeaders.Remove(HeaderNames.Authorization);
            var response = await _httpClient.GetAsync(documentUrl, cancellationToken);

            var debug = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode == false)
                throw new DomainException(this, InfraErrors.SignDoc.ErrorInRecoverDocSigned(documentUnitId));

            var docStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            return new DocSignedModel(Guid.Parse(documentUnitId), docStream, "PDF");
        }

        private async Task DeleteDocument(String doc_token, CancellationToken cancellationToken = default)
        {
            const int maxRetries = 6;
            int retryCount = 0;
            HttpResponseMessage response;

            do
            {
                response = await _httpClient.DeleteAsync($"/api/v1/docs/{doc_token}");

                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken); // Exponential backoff  
            }
            while (retryCount < maxRetries);
            var debug = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to delete document to ZapSign . DocToken: {doc_token}, Response: {Response}", doc_token, debug);
        }

        private async Task<bool> PlaceSignature(string docToken, string signerToken, PlaceSignature[] placeSignatures, CancellationToken cancellationToken = default)
        {
            var placeSignaturesJson = new JsonArray();
            foreach (var placeSignature in placeSignatures)
            {
                placeSignaturesJson.Add(
                    new JsonObject
                    {
                        ["page"] = placeSignature.Page.Value,
                        ["relative_position_bottom"] = placeSignature.RelativePositionBotton.Value,
                        ["relative_position_left"] = placeSignature.RelativePositionLeft.Value,
                        ["relative_size_x"] = placeSignature.RelativeSizeX.Value,
                        ["relative_size_y"] = placeSignature.RelativeSizeY.Value,
                        ["type"] = $"{ConvertSignatureType(placeSignature.Type)}",
                        ["signer_token"] = $"{signerToken}"
                    }
                );
            }

            var rubricas = new JsonObject
            {
                ["rubricas"] = placeSignaturesJson
            };

            const int maxRetries = 6;
            int retryCount = 0;
            HttpResponseMessage response;

            do
            {
                response = await _httpClient.PostAsJsonAsync($"/api/v1/docs/{docToken}/place-signatures/", rubricas, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken); // Exponential backoff  
            }
            while (retryCount < maxRetries);

            var debug = await response.Content.ReadAsStringAsync();

            return false;
        }

        private record SignerOptions(SignatureType AuthMode, bool RequireSelfiePhoto, bool LockPhone, bool LockEmail)
        {

        }

        private class SignatureType(int id, string name, string jsonName) : Enumeration(id, name)
        {
            public string JsonName { get; } = jsonName;

            public static readonly SignatureType DigitalSignature = new(1, nameof(DigitalSignature), "assinaturaTela");
            public static readonly SignatureType DigitalSignatureAndWhatsapp = new(2, nameof(DigitalSignatureAndWhatsapp), "assinaturaTela-tokenWhatsapp");
        }

        public async Task<string?> CreateWebHookToDocSignedEvent(string? docToken = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var authorizationToken = await _authorizationService.GetAuthorizationToken();

                var header = new JsonObject
                {
                    ["name"] = "Authorization",
                    ["value"] = $"Bearer {authorizationToken}"
                };

                var headersArray = new JsonArray() { header };

                var body = new JsonObject
                {
                    ["url"] = $"{_signOptions.WeebHookUrl}",
                    ["type"] = "doc_signed",
                    ["headers"] = headersArray
                };

                if (docToken != null)
                {
                    body = new JsonObject
                    {
                        ["url"] = $"{_signOptions.WeebHookUrl}",
                        ["type"] = "doc_signed",
                        ["doc_token"] = $"{docToken}",
                        ["headers"] = headersArray
                    };
                }


                const int maxRetries = 16;
                int retryCount = 0;
                HttpResponseMessage response;

                do
                {
                    response = await _httpClient.PostAsJsonAsync("/api/v1/user/company/webhook", body, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cancellationToken);
                        var id = json?["id"]?.ToString();
                        return id;
                    }

                    retryCount++;
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken); // Exponential backoff  
                }
                while (retryCount < maxRetries);


                var content = await response.Content.ReadAsStringAsync();
                if(response.IsSuccessStatusCode == false)
                    _logger.LogInformation("Failed to create webhook for document signed event. DocToken: {DocToken}, Url: {Url}, Response: {Response}",
                        docToken, _signOptions.WeebHookUrl, content);
                return null;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error creating webhook for document signed event. DocToken: {DocToken}, Url: {Url}", docToken, _signOptions.WeebHookUrl);
                return null;
            }
            
        }

        public async Task<bool> DeleteWebHook(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new JsonObject
                {
                    ["id"] = id
                };

                var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/user/company/webhook/delete/")
                {
                    Content = new StringContent(body.ToString(), System.Text.Encoding.UTF8, "application/json")
                };

               
                const int maxRetries = 16;
                int retryCount = 0;
                HttpResponseMessage response;

                do
                {
                    response = await _httpClient.SendAsync(request, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }

                    retryCount++;
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken); // Exponential backoff  
                }
                while (retryCount < maxRetries);

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to delete webhook. Id: {Id}, Response: {Response}", id, content);
                    return false;
                }

                _logger.LogInformation("Webhook deleted successfully. Id: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting webhook. Id: {Id}", id);
                return false;
            }
        }

        public async Task RefreshWebHookDocSignedEvent(CancellationToken cancellationToken = default)
        {
            try
            {
                var webHookId = await CreateWebHookToDocSignedEvent(cancellationToken: cancellationToken);

                if (string.IsNullOrEmpty(webHookId))
                {
                    _logger.LogError("Failed to create or refresh webhook for document signed event.");
                    return;
                }

                var webHook = await _webHookRepository.FirstOrDefaultAsync(x => x.Event == WebHookEvent.DocSigned, cancellation: cancellationToken);

                if (webHook is not null && string.IsNullOrEmpty(webHook.WebHookId) == false)
                {
                    await DeleteWebHook(webHook.WebHookId, cancellationToken);
                    webHook.WebHookId = webHookId;
                }
                else if (webHook is null)
                {
                    webHook = new WebHook(Guid.NewGuid(), webHookId, WebHookEvent.DocSigned);
                    await _webHookRepository.InsertAsync(webHook);
                }
                else
                {
                    webHook.WebHookId = webHookId;
                }
                await _webHookRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing webhook for document signed event.");
            }
        }

        private static string ConvertStreamToBase64(Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using MemoryStream ms = new();

            stream.CopyTo(ms);
            byte[] byteArray = ms.ToArray();
            return Convert.ToBase64String(byteArray);    
        }

        private static string ConvertSignatureType(TypeSignature typeSignature)
        {
            if (typeSignature == TypeSignature.Visa)
                return "visto";

            return "signature";
        }

        
    }

    

    

    

   
}
