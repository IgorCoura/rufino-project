namespace BillPayment.Infra.Asaas;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using Microsoft.Extensions.Logging;

/// <summary>
/// Consulta o título em <c>POST /v3/bill/simulate</c> e traduz a resposta para
/// <see cref="LookupSnapshot"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Este cliente HTTP retenta; o de pagamento não pode.</strong> A simulação é read-only
/// e idempotente, então repetir depois de um timeout é seguro. O adapter de pagamento da fase 3
/// precisa de um cliente próprio, sem retry automático — sobretudo o de Pix, cujo endpoint não
/// documenta idempotência nenhuma e pagaria duas vezes numa retentativa de rede
/// (ver <c>04-integrations.md</c>).
/// </para>
/// <para>
/// <strong>Campo ausente vira ausência, nunca zero.</strong> A cobertura medida na sprint 1.0 é
/// esburacada por natureza de documento; preencher com padrão faria o check comparar contra um
/// valor que o provedor nunca afirmou.
/// </para>
/// </remarks>
internal sealed class AsaasBillLookupService(
    HttpClient http,
    TimeProvider clock,
    ILogger<AsaasBillLookupService> logger) : IBillLookupService
{
    private const string SIMULATE_PATH = "bill/simulate";

    public async Task<BillLookupResult> SimulateAsync(DigitableLine digitableLine, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(digitableLine);

        var attemptedAt = clock.GetUtcNow();

        var (body, failure) = await http.PostAsync<AsaasSimulateResponse>(
            SIMULATE_PATH,
            new { identificationField = digitableLine.Value },
            logger,
            cancellationToken);

        if (failure is not null)
            return failure.IsRetryable
                ? BillLookupResult.Unavailable(failure.ReasonCode, failure.Message, attemptedAt)
                : BillLookupResult.Unresolved(failure.ReasonCode, failure.Message, attemptedAt);

        var info = body!.BankSlipInfo;
        if (info is null)
            return BillLookupResult.Unresolved("empty_bank_slip_info", null, attemptedAt);

        // beneficiaryName é a razão social; companyName é o nome comercial — e em arrecadação
        // costuma ser o único que volta. Não os colapse: perder a distinção tiraria do check
        // a chance de casar com um apelido aprendido do Payee.
        LookupParty beneficiary;
        try
        {
            beneficiary = LookupParty.From(info.BeneficiaryName, info.CompanyName, info.BeneficiaryCpfCnpj);
        }
        catch (DomainException)
        {
            return BillLookupResult.Unresolved("beneficiary_not_identified", null, attemptedAt);
        }

        return BillLookupResult.Resolved(
            LookupSnapshot.Create(
                beneficiary,
                attemptedAt,
                bankCode: AsaasHttp.ReadBankCode(info.Bank),
                amount: AsaasHttp.ReadMoney(info.Value),
                originalAmount: AsaasHttp.ReadMoney(info.OriginalValue),
                interest: AsaasHttp.ReadMoney(info.InterestValue),
                fine: AsaasHttp.ReadMoney(info.FineValue),
                discount: AsaasHttp.ReadMoney(info.DiscountValue),
                minAmount: AsaasHttp.ReadMoney(info.MinValue),
                maxAmount: AsaasHttp.ReadMoney(info.MaxValue),
                allowChangeValue: info.AllowChangeValue ?? false,
                dueDate: AsaasHttp.ReadDate(info.DueDate),
                isOverdue: info.IsOverdue ?? false,
                fee: AsaasHttp.ReadMoney(body.Fee),
                minimumScheduleDate: AsaasHttp.ReadDate(body.MinimumScheduleDate)),
            attemptedAt);
    }
}
