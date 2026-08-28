namespace BillPayment.Infra.Asaas;

using System.Globalization;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Decodifica o BR Code em <c>POST /v3/pix/qrCodes/decode</c> e traduz para
/// <see cref="PixLookupSnapshot"/>.
/// </summary>
/// <remarks>
/// É a única fonte do CPF/CNPJ do recebedor — o BR Code carrega chave e nome, nunca documento.
/// Sem esta chamada o trilho Pix não tem check de beneficiário.
/// Doc: https://docs.asaas.com/reference/decodificar-um-qrcode-para-pagamento
/// </remarks>
internal sealed class AsaasPixLookupService(
    HttpClient http,
    TimeProvider clock,
    ILogger<AsaasPixLookupService> logger) : IPixLookupService
{
    private const string DECODE_PATH = "pix/qrCodes/decode";
    private const string DYNAMIC_TYPE = "DYNAMIC";
    private const string NATURAL_PERSON = "FISICA";
    private const string LEGAL_PERSON = "JURIDICA";

    public async Task<PixLookupResult> DecodeAsync(
        PixPayload payload,
        DateOnly? expectedPaymentDate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var attemptedAt = clock.GetUtcNow();

        var (body, failure) = await http.PostAsync<AsaasPixDecodeResponse>(
            DECODE_PATH,
            new AsaasPixDecodeRequest
            {
                Payload = payload.Payload,
                ExpectedPaymentDate = expectedPaymentDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            },
            logger,
            cancellationToken);

        if (failure is not null)
            return failure.IsRetryable
                ? PixLookupResult.Unavailable(failure.ReasonCode, failure.Message, attemptedAt)
                : PixLookupResult.Unresolved(failure.ReasonCode, failure.Message, attemptedAt);

        var receiverData = body!.Receiver;
        if (receiverData is null)
            return PixLookupResult.Unresolved("empty_receiver", null, attemptedAt);

        LookupParty receiver;
        try
        {
            receiver = LookupParty.From(receiverData.Name, receiverData.TradingName, receiverData.CpfCnpj);
        }
        catch (DomainException)
        {
            return PixLookupResult.Unresolved("receiver_not_identified", null, attemptedAt);
        }

        return PixLookupResult.Resolved(
            PixLookupSnapshot.Create(
                receiver,
                attemptedAt,
                canBePaid: body.CanBePaid ?? true,
                cannotBePaidReason: body.CannotBePaidReason,
                isDynamic: string.Equals(body.Type, DYNAMIC_TYPE, StringComparison.OrdinalIgnoreCase),
                receiverIspb: receiverData.Ispb,
                receiverIspbName: receiverData.IspbName,
                receiverKind: ReadPersonKind(receiverData.PersonType),
                amount: AsaasHttp.ReadMoney(body.Value),
                totalAmount: AsaasHttp.ReadMoney(body.TotalValue),
                interest: AsaasHttp.ReadMoney(body.Interest),
                fine: AsaasHttp.ReadMoney(body.Fine),
                discount: AsaasHttp.ReadMoney(body.Discount),
                changeAmount: AsaasHttp.ReadMoney(body.ChangeValue),
                dueDate: AsaasHttp.ReadDate(body.DueDate),
                expirationDate: AsaasHttp.ReadTimestamp(body.ExpirationDate),
                canBePaidWithDifferentValue: body.CanBePaidWithDifferentValue ?? false,
                conciliationIdentifier: body.ConciliationIdentifier,
                payer: ReadPayer(body.Payer),
                description: body.Description),
            attemptedAt);
    }

    private static TaxIdKind? ReadPersonKind(string? personType)
        => personType?.Trim().ToUpperInvariant() switch
        {
            NATURAL_PERSON => TaxIdKind.CPF,
            LEGAL_PERSON => TaxIdKind.CNPJ,
            _ => null,
        };

    /// <summary>
    /// O pagador chega com o documento mascarado. Máscara sem nenhum dígito visível não
    /// contradiz nada, e o VO recusa guardá-la — aqui isso vira ausência de pagador, não falha
    /// da consulta inteira.
    /// </summary>
    private static MaskedParty? ReadPayer(AsaasPixParty? payer)
    {
        if (payer is null || (string.IsNullOrWhiteSpace(payer.Name) && string.IsNullOrWhiteSpace(payer.CpfCnpj)))
            return null;

        try
        {
            return MaskedParty.Of(payer.Name, payer.CpfCnpj);
        }
        catch (DomainException)
        {
            return string.IsNullOrWhiteSpace(payer.Name) ? null : MaskedParty.Of(payer.Name, maskedTaxId: null);
        }
    }
}
