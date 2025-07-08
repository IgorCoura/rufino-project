using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Net.Http.Headers;
using Microsoft.Net.Http.Headers;
using static System.Net.WebRequestMethods;
using System.Threading;
using PeopleManagement.Domain.SeedWord;
using Microsoft.Extensions.Logging;

namespace PeopleManagement.Infra.Services
{
    public class ZapSignService(HttpClient httpClient, ILogger<ZapSignService> logger) : ISignService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<ZapSignService> _logger = logger;

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

            var folderPath = $"{company.CorporateName}/{employee.Name}";

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

            var debug = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode == false)
                throw new DomainException(this, InfraErrors.SignDoc.ErrorSendDocToSign(documentUnitId));

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
            _logger.LogInformation("Document sent to ZapSign for signature successfully. DocumentUnitId: {DocumentUnitId}, EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                documentUnitId, employee.Id, company.Id);
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
                response = await _httpClient.DeleteAsync("https://api.zapsign.com.br/api/v1/docs/{{doc_token}}/");

                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken); // Exponential backoff  
            }
            while (retryCount < maxRetries);
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

        private async Task<bool> CreateWebHookToDocSigned(string docToken, string url, string authorizationToken, CancellationToken cancellationToken = default)
        {
            var headers = new JsonObject
            {
                ["name"] = "Authorization",
                ["value"] = $"{authorizationToken}"
            };

            var headersArray = new JsonArray();
            headersArray.Add(headers);

            var body = new JsonObject
            {
                ["url"] = $"{url}",
                ["type"] = "doc_signed",
                ["doc_token"] = $"{docToken}",
                ["headers"] = headersArray
            };
            var response = await _httpClient.PostAsJsonAsync("api/v1/user/company/webhook/", body, cancellationToken);
            return response.IsSuccessStatusCode;
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
