namespace BillPayment.Domain.Services;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;

/// <summary>
/// O que a recaptura de um e-mail pode e não pode desfazer, olhando os boletos que os anexos
/// dele produziram.
/// </summary>
/// <remarks>
/// <para>
/// A regra cruza três agregados — o registro do e-mail, os itens e os boletos — e por isso mora
/// num Domain Service, não em nenhum deles. Ela existe porque recapturar é "refaça a triagem do
/// zero", e refazer do zero apaga o item e recria o boleto: <strong>um boleto com dinheiro já
/// comprometido não se refaz por trás de quem decidiu</strong> (<c>BLP.CMS11</c>).
/// </para>
/// <para>
/// O que ainda não foi decidido (<c>Captured</c>, <c>AwaitingApproval</c>) é cancelado para a
/// triagem nova recriá-lo — a chave única do instrumento é liberada pelo cancelamento. O que foi
/// <c>Denied</c> não bloqueia, mas é devolvido como aviso: a pessoa que pediu a recaptura precisa
/// saber que aquele boleto já tinha sido negado uma vez. <c>Cancelled</c> não pede nada.
/// </para>
/// </remarks>
public static class MessageRecaptureService
{
    public static RecapturePlan Plan(
        CapturedMessage message,
        IReadOnlyCollection<(CaptureItem Item, Bill? Bill)> linked)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(linked);

        message.EnsureCanBeRecaptured();

        var toCancel = new List<Bill>();
        var previouslyDenied = new List<BillId>();

        foreach (var (item, bill) in linked)
        {
            if (bill is null)
                continue;

            // Só o boleto que ESTE anexo produziu conta. Um item que aponte para outro boleto —
            // reenvio apontando para o original, promovido por outro e-mail — não é deste e-mail.
            if (item.BillId is not { } billId || !billId.Equals(bill.Id))
                continue;

            if (bill.Status.IsCommittedToPayment)
                throw CapturedMessageErrors.RecaptureBlockedByDecidedBill(bill.Id.Value);

            if (bill.Status == BillStatus.Denied)
                previouslyDenied.Add(bill.Id);
            else if (bill.Status != BillStatus.Cancelled)
                toCancel.Add(bill);
        }

        return new RecapturePlan(toCancel, previouslyDenied);
    }
}

/// <summary>
/// O que o handler executa: os boletos pendentes a cancelar e os avisos a devolver.
/// </summary>
public sealed record RecapturePlan(
    IReadOnlyList<Bill> BillsToCancel,
    IReadOnlyList<BillId> PreviouslyDeniedBills);
