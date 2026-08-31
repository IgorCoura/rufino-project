namespace BillPayment.Domain.Payees;

using BillPayment.Domain.SeedWork;

/// <summary>
/// A marca de confiança que o tenant dá ao beneficiário. <see cref="Blacklisted"/> é a única
/// com efeito na verificação — todo boleto do beneficiário reprova o check de beneficiário
/// como falha bloqueante e nasce Perigo (ADR-015: quem decide continua sendo o humano, com
/// aceite explícito). <see cref="Whitelisted"/> é organização visual, sem efeito de régua:
/// afrouxar verificação por marca criaria um alvo — comprometer um beneficiário marcado.
/// </summary>
public sealed class PayeeStanding : Enumeration
{
    public static readonly PayeeStanding Normal = new(1, nameof(Normal));
    public static readonly PayeeStanding Whitelisted = new(2, nameof(Whitelisted));
    public static readonly PayeeStanding Blacklisted = new(3, nameof(Blacklisted));

    private PayeeStanding(int id, string name) : base(id, name) { }
}
