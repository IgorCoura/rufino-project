using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Models;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Options;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.SeedWord;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace PeopleManagement.Infra.Services;

//REFERENCE API: https://developers.clicksign.com/reference
//DOC API: https://developers.clicksign.com/docs
public class ClickSignDocumentSignatureService : IDocumentSignatureService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClickSignDocumentSignatureService> _logger;
    private readonly SignOptions _options;

    public ClickSignDocumentSignatureService(
        HttpClient httpClient,
        ILogger<ClickSignDocumentSignatureService> logger,
        SignOptions options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;
    }

    // ── Métodos públicos (mantém a mesma assinatura da interface) ──────────

    public Task<DocumentSignatureModel> SendToSignatureWithWhatsapp(
        Stream documentStream, Guid documentUnitId, Document document,
        Company company, Employee employee, PlaceSignature[] placeSignatures,
        DateTime dateLimitToSign, int eminderEveryNDays,
        CancellationToken cancellationToken = default)
        => SendToSignature(
            new SignerOptions([AuthMode.Handwritten, AuthMode.Whatsapp]),
            documentStream, documentUnitId, document, company, employee,
            placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);

    public Task<DocumentSignatureModel> SendToSignatureWithSMS(
        Stream documentStream, Guid documentUnitId, Document document,
        Company company, Employee employee, PlaceSignature[] placeSignatures,
        DateTime dateLimitToSign, int eminderEveryNDays,
        CancellationToken cancellationToken = default)
        => SendToSignature(
            new SignerOptions([AuthMode.Handwritten, AuthMode.Sms]),
            documentStream, documentUnitId, document, company, employee,
            placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);

    public Task<DocumentSignatureModel> SendToSignatureWithSelfie(
        Stream documentStream, Guid documentUnitId, Document document,
        Company company, Employee employee, PlaceSignature[] placeSignatures,
        DateTime dateLimitToSign, int eminderEveryNDays,
        CancellationToken cancellationToken = default)
        => SendToSignature(
            new SignerOptions([AuthMode.FacialBiometrics]),
            documentStream, documentUnitId, document, company, employee,
            placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);

    public Task<DocumentSignatureModel> SendToSignatureWithOnlyWhatsapp(
        Stream documentStream, Guid documentUnitId, Document document,
        Company company, Employee employee, PlaceSignature[] placeSignatures,
        DateTime dateLimitToSign, int eminderEveryNDays,
        CancellationToken cancellationToken = default)
        => SendToSignature(
            new SignerOptions([AuthMode.Whatsapp]),
            documentStream, documentUnitId, document, company, employee,
            placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);

    public Task<DocumentSignatureModel> SendToSignatureWithOnlySMS(
        Stream documentStream, Guid documentUnitId, Document document,
        Company company, Employee employee, PlaceSignature[] placeSignatures,
        DateTime dateLimitToSign, int eminderEveryNDays,
        CancellationToken cancellationToken = default)
        => SendToSignature(
            new SignerOptions([AuthMode.Sms]),
            documentStream, documentUnitId, document, company, employee,
            placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);

    // ── Orquestrador principal ─────────────────────────────────────────────

    private async Task<DocumentSignatureModel> SendToSignature(
        SignerOptions signerOptions,
        Stream documentStream, Guid documentUnitId, Document document,
        Company company, Employee employee, PlaceSignature[] placeSignatures,
        DateTime dateLimitToSign, int eminderEveryNDays,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending document to ClickSign. DocumentUnitId: {DocumentUnitId}, EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
            documentUnitId, employee.Id, company.Id);

        // 1. Criar Envelope
        var envelopeKey = await CreateEnvelope(document, company, dateLimitToSign, eminderEveryNDays, cancellationToken);

        try
        {
            // 2. Upload do Documento
            var documentKey = await UploadDocument(envelopeKey, document, documentUnitId, documentStream, cancellationToken);

            // 3. Criar Signatário
            var (signerKey, signerUrl) = await CreateSigner(envelopeKey, employee, cancellationToken);

            // 4. Criar Requisitos (qualificação + autenticação)
            await CreateRequirements(envelopeKey, documentKey, signerKey, signerOptions, cancellationToken);

            // 5. Criar Requisitos de Rubrica (posicionamento)
            //if (placeSignatures.Length > 0)
            //    await CreateRubricRequirements(envelopeKey, documentKey, signerKey, placeSignatures, cancellationToken);

            // 6. Ativar Envelope
            await ActivateEnvelope(envelopeKey, cancellationToken);

            // 7. Notificar Signatário
            //await NotifySigner(envelopeKey, signerKey, cancellationToken);

            _logger.LogInformation(
                "Document sent to ClickSign successfully. DocumentUnitId: {DocumentUnitId}, EnvelopeKey: {EnvelopeKey}, SignerUrl: {SignerUrl}",
                documentUnitId, envelopeKey, signerUrl);

            return new DocumentSignatureModel(envelopeKey, signerUrl);
        }
        catch
        {
            // Rollback: exclui o envelope em caso de falha
            await DeleteEnvelope(envelopeKey, cancellationToken);
            throw;
        }
    }

    // ── Etapas individuais ─────────────────────────────────────────────────

    private async Task<string> CreateEnvelope(
        Document document, Company company,
        DateTime dateLimitToSign, int reminderEveryNDays,
        CancellationToken cancellationToken)
    {
        var attributes = new JsonObject
        {
            ["name"] = $"{document.Name} - {DateOnly.FromDateTime(DateTime.UtcNow)}",
            ["locale"] = "pt-BR",
            ["auto_close"] = true,
            ["deadline_at"] = dateLimitToSign.ToString("o"),
            ["block_after_refusal"] = true
        };

        if (reminderEveryNDays > 0)
            attributes["remind_interval"] = reminderEveryNDays;

        var body = new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["type"] = "envelopes",
                ["attributes"] = attributes
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v3/envelopes?access_token={_options.AccessToken}", body, cancellationToken);

        await EnsureSuccessAsync(response, "criar envelope", cancellationToken);

        var content = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cancellationToken);
        return content?["data"]?["id"]?.ToString()
            ?? throw new InvalidOperationException("ClickSign não retornou o envelope_key.");
    }

    private async Task<string> UploadDocument(
        string envelopeKey, Document document, Guid documentUnitId,
        Stream documentStream, CancellationToken cancellationToken)
    {
        var base64 = ConvertStreamToBase64(documentStream);

        var body = new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["type"] = "documents",
                ["attributes"] = new JsonObject
                {
                    ["filename"] = $"{document.Name}.pdf",
                    ["content_base64"] = $"data:application/pdf;base64,{base64}"
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v3/envelopes/{envelopeKey}/documents?access_token={_options.AccessToken}", body, cancellationToken);

        await EnsureSuccessAsync(response, "fazer upload do documento", cancellationToken);

        var content = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cancellationToken);
        return content?["data"]?["id"]?.ToString()
            ?? throw new InvalidOperationException("ClickSign não retornou o document_key.");
    }

    private async Task<(string signerKey, string signerUrl)> CreateSigner(
        string envelopeKey, Employee employee, CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["type"] = "signers",
                ["attributes"] = new JsonObject
                {
                    ["name"] = $"{employee.Name}",
                    ["email"] = $"{employee.Contact?.Email ?? ""}",
                    ["phone_number"] = $"{employee.Contact!.CellPhone}",
                    ["documentation"] = $"{employee.IdCard?.Cpf.ToMaskedString() ?? ""}",
                    ["communicate_events"] = new JsonObject
                    {
                        ["signature_request"] = "whatsapp",
                        ["document_signed"] = "whatsapp",

                    }


                }
                
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v3/envelopes/{envelopeKey}/signers?access_token={_options.AccessToken}", body, cancellationToken);

        await EnsureSuccessAsync(response, "criar signatário", cancellationToken);

        var content = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: cancellationToken);
        var signerKey = content?["data"]?["id"]?.ToString()
            ?? throw new InvalidOperationException("ClickSign não retornou o signer_key.");
        var signerUrl = content?["data"]?["attributes"]?["url"]?.ToString() ?? "";

        return (signerKey, signerUrl);
    }

    private async Task CreateRequirements(
        string envelopeKey, string documentKey, string signerKey,
        SignerOptions signerOptions, CancellationToken cancellationToken)
    {
        // Requisito de Qualificação (obrigatório)
        await CreateQualificationRequirement(envelopeKey, documentKey, signerKey, cancellationToken);

        // Requisitos de Autenticação (um por modo)
        foreach (var authMode in signerOptions.AuthModes)
            await CreateAuthenticationRequirement(envelopeKey, documentKey, signerKey, authMode, cancellationToken);
    }

    private async Task CreateQualificationRequirement(
        string envelopeKey, string documentKey, string signerKey,
        CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["type"] = "requirements",
                ["attributes"] = new JsonObject
                {
                    ["action"] = "agree",
                    ["role"] = "sign"
                },
                ["relationships"] = new JsonObject
                {
                    ["document"] = new JsonObject
                    {
                        ["data"] = new JsonObject { ["type"] = "documents", ["id"] = documentKey }
                    },
                    ["signer"] = new JsonObject
                    {
                        ["data"] = new JsonObject { ["type"] = "signers", ["id"] = signerKey }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v3/envelopes/{envelopeKey}/requirements?access_token={_options.AccessToken}", body, cancellationToken);

        await EnsureSuccessAsync(response, "criar requisito de qualificação", cancellationToken);
    }

    private async Task CreateAuthenticationRequirement(
        string envelopeKey, string documentKey, string signerKey,
        AuthMode authMode, CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["type"] = "requirements",
                ["attributes"] = new JsonObject
                {
                    ["action"] = "provide_evidence",
                    ["auth"] = authMode.ApiValue
                },
                ["relationships"] = new JsonObject
                {
                    ["document"] = new JsonObject
                    {
                        ["data"] = new JsonObject { ["type"] = "documents", ["id"] = documentKey }
                    },
                    ["signer"] = new JsonObject
                    {
                        ["data"] = new JsonObject { ["type"] = "signers", ["id"] = signerKey }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v3/envelopes/{envelopeKey}/requirements?access_token={_options.AccessToken}", body, cancellationToken);

        await EnsureSuccessAsync(response, $"criar requisito de autenticação ({authMode.ApiValue})", cancellationToken);
    }

    private async Task CreateRubricRequirements(
        string envelopeKey, string documentKey, string signerKey,
        PlaceSignature[] placeSignatures, CancellationToken cancellationToken)
    {
        foreach (var place in placeSignatures)
        {
            var body = new JsonObject
            {
                ["data"] = new JsonObject
                {
                    ["type"] = "requirements",
                    ["attributes"] = new JsonObject
                    {
                        ["action"] = "rubricate",
                        ["kind"] = "initials",
                        ["page"] = "0",
                    },
                    ["relationships"] = new JsonObject
                    {
                        ["document"] = new JsonObject
                        {
                            ["data"] = new JsonObject { ["type"] = "documents", ["id"] = documentKey }
                        },
                        ["signer"] = new JsonObject
                        {
                            ["data"] = new JsonObject { ["type"] = "signers", ["id"] = signerKey }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v3/envelopes/{envelopeKey}/requirements?access_token={_options.AccessToken}", body, cancellationToken);

            await EnsureSuccessAsync(response, "criar requisito de rubrica", cancellationToken);
        }
    }

    private async Task ActivateEnvelope(string envelopeKey, CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["type"] = "envelopes",
                ["id"] = envelopeKey,
                ["attributes"] = new JsonObject { ["status"] = "running" }
            }
        };

        var response = await _httpClient.PatchAsJsonAsync(
            $"/api/v3/envelopes/{envelopeKey}?access_token={_options.AccessToken}", body, cancellationToken);

        await EnsureSuccessAsync(response, "ativar envelope", cancellationToken);
    }

    private async Task NotifySigner(string envelopeKey, string signerKey, CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["type"] = "notifications",
                ["relationships"] = new JsonObject
                {
                    ["signer"] = new JsonObject
                    {
                        ["data"] = new JsonObject { ["type"] = "signers", ["id"] = signerKey }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v3/envelopes/{envelopeKey}/notifications?access_token={_options.AccessToken}", body, cancellationToken);

        // Notificação não é crítica — apenas log em caso de falha
        if (!response.IsSuccessStatusCode)
        {
            var debug = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to notify signer. EnvelopeKey: {EnvelopeKey}, SignerKey: {SignerKey}, Response: {Response}",
                envelopeKey, signerKey, debug);
        }
    }

    private async Task DeleteEnvelope(string envelopeKey, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"/api/v3/envelopes/{envelopeKey}?access_token={_options.AccessToken}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var debug = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to delete envelope from ClickSign. EnvelopeKey: {EnvelopeKey}, StatusCode: {StatusCode}, Response: {Response}",
                    envelopeKey, response.StatusCode, debug);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting envelope. EnvelopeKey: {EnvelopeKey}", envelopeKey);
        }
    }

    // ── Métodos de sessão (não suportados pelo ClickSign — NotSupportedException) ──

    public Task<AttachmentResultModel> AddDocumentAttachment(string primaryDocToken, Stream documentStream, string documentName,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("ClickSign does not support adding attachments to existing documents.");

    public Task<bool> PlaceSignatureOnAttachment(string primaryDocToken, string signerToken, PlaceSignature[] placeSignatures,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("ClickSign does not support placing signatures on attachments.");

    public Task<SessionSignedDocumentsModel> GetSessionSignedDocuments(string primaryDocToken,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("ClickSign does not support session signed documents retrieval.");

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var debug = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("ClickSign error on [{Operation}]. StatusCode: {StatusCode}, Response: {Response}",
                operation, response.StatusCode, debug);
            throw new DomainException(this, InfraErrors.SignDoc.ErrorSendDocToSign(Guid.Empty));
        }
    }

    private static string ConvertStreamToBase64(Stream stream)
    {
        if (stream.CanSeek) stream.Position = 0;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Convert.ToBase64String(ms.ToArray());
    }

    private static string ConvertSignatureType(TypeSignature typeSignature)
        => typeSignature == TypeSignature.Visa ? "initials" : "manuscript";

    // ── Tipos internos ─────────────────────────────────────────────────────

    private record SignerOptions(AuthMode[] AuthModes);

    private class AuthMode(string apiValue) : Enumeration(0, apiValue)
    {
        public string ApiValue { get; } = apiValue;

        public static readonly AuthMode Handwritten      = new("handwritten");
        public static readonly AuthMode Whatsapp         = new("whatsapp");
        public static readonly AuthMode Sms              = new("sms");
        public static readonly AuthMode FacialBiometrics = new("facial_biometrics");
        public static readonly AuthMode Email            = new("email");
    }
}