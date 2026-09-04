namespace BillPayment.Domain.Instruments;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Por qual trilho o documento vai ser pago. Decidido pelo agregado a partir dos
/// instrumentos disponíveis, nunca escolhido por quem chama.
/// </summary>
/// <remarks>
/// Havendo QR Pix válido, paga-se por Pix — decisão do <c>ADR-010</c>. A precedência mora
/// aqui, e não espalhada em <c>if</c>, para que mudá-la seja mudar um número.
/// </remarks>
public sealed class PaymentRail : Enumeration
{
    /// <summary>Trilho preferencial: liquidação imediata, agendável e cancelável.</summary>
    public static readonly PaymentRail Pix = new(1, "Pix", precedence: 1);

    /// <summary>Usado quando o documento só traz código de barras.</summary>
    public static readonly PaymentRail Boleto = new(2, "Boleto", precedence: 2);

    /// <summary>Menor vence. Ver <c>ADR-010</c>.</summary>
    public int Precedence { get; }

    private PaymentRail(int id, string name, int precedence) : base(id, name)
        => Precedence = precedence;
}
