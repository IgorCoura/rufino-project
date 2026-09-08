namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>
/// De ONDE partiu a ação registrada na trilha do boleto.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque "quem" não basta.</strong> Até 2026-09-08 toda mudança vinda do lado do
/// pagamento entrava na trilha como autor "Sistema" — e isso tornava indistinguíveis três fatos
/// muito diferentes: alguém cancelou pelo nosso app, alguém cancelou <em>no painel do provedor</em>,
/// e o próprio provedor cancelou. Quem lesse a trilha meses depois não teria como saber quem
/// praticou o ato, e é exatamente essa dúvida que uma trilha existe para não deixar acontecer.
/// </para>
/// <para>
/// <strong><see cref="Provider"/> deliberadamente NÃO separa "painel" de "decisão do provedor".</strong>
/// A API dele devolve o mesmo <c>CANCELLED</c> nos dois casos — não há campo que os distinga.
/// Inventar a distinção seria afirmar na trilha de auditoria algo que não foi medido; dizer
/// "partiu do provedor" é tudo o que se pode afirmar com honestidade, e já é a informação que
/// faltava.
/// </para>
/// </remarks>
public sealed class BillActionOrigin : Enumeration
{
    /// <summary>
    /// Uma pessoa, pela nossa API. A entrada de trilha traz o <c>UserId</c> e o nome.
    /// </summary>
    public static readonly BillActionOrigin User = new(1, "User");

    /// <summary>
    /// O provedor de pagamento — o painel dele, ou uma decisão dele. Sem autor identificável.
    /// </summary>
    public static readonly BillActionOrigin Provider = new(2, "Provider");

    /// <summary>
    /// Automação nossa: outbox, fila de submissão, conciliação, revalidação. Sem autor.
    /// </summary>
    public static readonly BillActionOrigin System = new(3, "System");

    private BillActionOrigin(int id, string name) : base(id, name) { }

    /// <summary>Se a origem admite um autor identificável.</summary>
    public bool CarriesActor => this == User;
}
