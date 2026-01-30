using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.SeedWord;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace PeopleManagement.Infra.Services
{
    public class ZapSignDocumentSignatureService : IDocumentSignatureService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ZapSignDocumentSignatureService> _logger;

        public ZapSignDocumentSignatureService(
            HttpClient httpClient,
            ILogger<ZapSignDocumentSignatureService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendToSignatureWithWhatsapp(Stream documentStream, Guid documentUnitId, Document document, Company company, 
            Employee employee, PlaceSignature[] placeSignatures, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default)
        {
            var signerOptions = new SignerOptions(SignatureType.DigitalSignatureAndWhatsapp, false, true, true);
            await SendToSignature(signerOptions, documentStream, documentUnitId, document, company, employee, placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);
        }
        public async Task SendToSignatureWithSMS(Stream documentStream, Guid documentUnitId, Document document, Company company, 
            Employee employee, PlaceSignature[] placeSignatures, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default)
        {
            var signerOptions = new SignerOptions(SignatureType.DigitalSignatureAndSMS, false, true, true);
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
                    ["email"] = $"{employee.Contact?.Email ?? ""}",
                    ["send_automatic_email"] = true,
                    ["send_automatic_whatsapp"] = false,
                    ["send_automatic_whatsapp_signed_file"] = false,
                    ["phone_country"] = $"55",
                    ["phone_number"] = $"{employee.Contact!.CellPhone}",
                    ["lock_email"] = signerOptions.LockEmail,
                    ["blank_email"] = true,
                    ["lock_phone"] = signerOptions.LockEmail,
                    ["blank_phone"] = true,
                    ["lock_name"] = true,
                    ["cpf"] = $"{employee.IdCard?.Cpf ?? ""}",
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

            var response = await _httpClient.PostAsync(
                "/api/v1/docs/", 
                new StringContent(documentToSign.ToString(), System.Text.Encoding.UTF8, "application/json"), 
                cancellationToken);

            if (response.IsSuccessStatusCode == false)
            {
                var debug = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send document to ZapSign for signature. DocumentUnitId: {DocumentUnitId}, EmployeeId: {EmployeeId}, CompanyId: {CompanyId}, StatusCode: {StatusCode}, Response: {Response}",
                    documentUnitId, employee.Id, company.Id, response.StatusCode, debug);
                throw new DomainException(this, InfraErrors.SignDoc.ErrorSendDocToSign(documentUnitId));
            }

            var content = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cancellationToken);
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

        private async Task DeleteDocument(string docToken, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/v1/docs/{docToken}", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var debug = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Failed to delete document from ZapSign. DocToken: {DocToken}, StatusCode: {StatusCode}, Response: {Response}",
                        docToken, response.StatusCode, debug);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document. DocToken: {DocToken}", docToken);
            }
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
                        ["type"] = ConvertSignatureType(placeSignature.Type),
                        ["signer_token"] = signerToken
                    }
                );
            }

            var rubricas = new JsonObject
            {
                ["rubricas"] = placeSignaturesJson
            };

            var response = await _httpClient.PostAsJsonAsync($"/api/v1/docs/{docToken}/place-signatures/", rubricas, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var debug = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to place signatures. DocToken: {DocToken}, StatusCode: {StatusCode}, Response: {Response}",
                    docToken, response.StatusCode, debug);
                return false;
            }

            return true;
        }

        private record SignerOptions(SignatureType AuthMode, bool RequireSelfiePhoto, bool LockPhone, bool LockEmail)
        {

        }

        private class SignatureType(int id, string name, string jsonName) : Enumeration(id, name)
        {
            public string JsonName { get; } = jsonName;

            public static readonly SignatureType DigitalSignature = new(1, nameof(DigitalSignature), "assinaturaTela");
            public static readonly SignatureType DigitalSignatureAndWhatsapp = new(2, nameof(DigitalSignatureAndWhatsapp), "assinaturaTela-tokenWhatsapp");
            public static readonly SignatureType DigitalSignatureAndSMS = new(3, nameof(DigitalSignatureAndSMS), "assinaturaTela-tokenSms");
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
