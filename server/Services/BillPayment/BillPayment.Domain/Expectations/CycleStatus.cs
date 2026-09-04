namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.SeedWork;

/// <summary>Em que pé está um ciclo de expectativa.</summary>
public sealed class CycleStatus : Enumeration
{
    /// <summary>Aberto, aguardando a conta.</summary>
    public static readonly CycleStatus Waiting = new(1, nameof(Waiting), isOpen: true);

    /// <summary>A conta chegou e virou boleto.</summary>
    public static readonly CycleStatus Fulfilled = new(2, nameof(Fulfilled), isOpen: false);

    /// <summary>Chegou algo e não deu para transformar em boleto.</summary>
    public static readonly CycleStatus PartiallyCaptured = new(3, nameof(PartiallyCaptured), isOpen: true);

    /// <summary>Passou da data de alerta sem cumprimento.</summary>
    public static readonly CycleStatus Missing = new(4, nameof(Missing), isOpen: true);

    /// <summary>O usuário disse que este ciclo não vem.</summary>
    public static readonly CycleStatus Waived = new(5, nameof(Waived), isOpen: false);

    /// <summary>
    /// Ainda comporta alerta e ainda pode ser cumprido. <c>PartiallyCaptured</c> e
    /// <c>Missing</c> continuam abertos de propósito: a conta pode ser destravada ou
    /// reivindicada depois, e fechar aí perderia o cumprimento tardio.
    /// </summary>
    public bool IsOpen { get; }

    private CycleStatus(int id, string name, bool isOpen) : base(id, name) => IsOpen = isOpen;
}
