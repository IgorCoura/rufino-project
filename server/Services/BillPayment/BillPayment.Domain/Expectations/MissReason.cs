namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.SeedWork;

/// <summary>Por que o ciclo não foi cumprido.</summary>
/// <remarks>
/// <strong>A separação em duas famílias é o que dá utilidade ao alerta</strong>, porque a ação do
/// usuário muda: quando nada chegou ele precisa ir buscar; quando chegou e não deu para ler, o
/// sistema já tem o documento e sabe o que falta — e o alerta leva direto ao item resolvível.
/// </remarks>
public sealed class MissReason : Enumeration
{
    /// <summary>Nada chegou pela janela do ciclo.</summary>
    public static readonly MissReason NeverArrived = new(1, nameof(NeverArrived), arrived: false);

    /// <summary>O portal não respondeu — fase 5.</summary>
    public static readonly MissReason PortalUnavailable = new(2, nameof(PortalUnavailable), arrived: false);

    /// <summary>Chegou, e a cascata não reconheceu boleto.</summary>
    public static readonly MissReason CaptureFailed = new(3, nameof(CaptureFailed), arrived: true);

    /// <summary>Chegou cifrado e nenhuma senha derivada abriu.</summary>
    public static readonly MissReason Locked = new(4, nameof(Locked), arrived: true);

    /// <summary>Chegou por link e a escada de resolução não trouxe o documento.</summary>
    public static readonly MissReason LinkFailed = new(5, nameof(LinkFailed), arrived: true);

    /// <summary>Chegou e foi lido, mas não se soube de quem era.</summary>
    public static readonly MissReason Unrouted = new(6, nameof(Unrouted), arrived: true);

    /// <summary>
    /// O documento chegou. Decide qual dos dois alertas o usuário recebe, e é a razão de este
    /// Smart Enum existir em vez de uma string.
    /// </summary>
    public bool Arrived { get; }

    private MissReason(int id, string name, bool arrived) : base(id, name) => Arrived = arrived;
}
