namespace BillPayment.Domain.Ports;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;

/// <summary>
/// Decodifica um QR Pix na instituição do recebedor. É a única forma de saber o CPF/CNPJ de
/// quem vai receber — o BR Code carrega chave e nome, nunca documento.
/// </summary>
public interface IPixLookupService
{
    /// <param name="expectedPaymentDate">
    /// Data prevista de pagamento. A instituição recalcula juros, multa e desconto para ela —
    /// informar hoje quando o pagamento será na semana que vem devolve um valor que não é o
    /// que será debitado, e o check de valor passaria a comparar contra o número errado.
    /// </param>
    Task<PixLookupResult> DecodeAsync(
        PixPayload payload,
        DateOnly? expectedPaymentDate,
        CancellationToken cancellationToken);
}
