namespace BillPayment.Domain.CaptureItems;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Onde o artefato ingerido está entre "acabou de chegar" e "virou boleto ou foi para a
/// quarentena". A máquina de estados inteira vive aqui — nenhum handler decide transição.
/// </summary>
/// <remarks>
/// <para>
/// Os cinco primeiros ids são o funil de processamento; os cinco últimos são os desfechos da
/// tabela de quarentena do doc 07. O catálogo é declarado inteiro desde já porque o id vai para
/// o banco, mesmo que os degraus de extração (<see cref="Locked"/>) e de link
/// (<see cref="LinkPending"/>, <see cref="LinkFailed"/>) só ganhem código nas sprints 2.3 e 2.5.
/// </para>
/// </remarks>
public sealed class CaptureItemStatus : Enumeration
{
    /// <summary>Ingerido e armazenado como veio. Ainda não se sabe se há boleto aqui.</summary>
    public static readonly CaptureItemStatus Received = new(1, nameof(Received));

    /// <summary>Um instrumento de pagamento válido foi extraído. Falta decidir de quem é.</summary>
    public static readonly CaptureItemStatus Parsed = new(2, nameof(Parsed));

    /// <summary>PDF cifrado que nenhum candidato de senha abriu; aguarda senha do usuário.</summary>
    public static readonly CaptureItemStatus Locked = new(3, nameof(Locked));

    /// <summary>Há um link para o documento e o download ainda não aconteceu.</summary>
    public static readonly CaptureItemStatus LinkPending = new(4, nameof(LinkPending));

    /// <summary>A escada de resolução de link esgotou sem trazer o documento.</summary>
    public static readonly CaptureItemStatus LinkFailed = new(5, nameof(LinkFailed));

    /// <summary>Roteou para este tenant e virou <c>Bill</c>.</summary>
    public static readonly CaptureItemStatus Promoted =
        new(6, nameof(Promoted), isTerminal: true, exposesFinancialDetail: true);

    /// <summary>O pagador foi identificado e <strong>não</strong> é deste tenant.</summary>
    public static readonly CaptureItemStatus ForeignPayer = new(7, nameof(ForeignPayer), isTerminal: true);

    /// <summary>Não foi possível determinar o dono. Fica na fila de reivindicação.</summary>
    public static readonly CaptureItemStatus Unrouted = new(8, nameof(Unrouted), exposesFinancialDetail: true);

    /// <summary>Nenhum boleto válido no artefato.</summary>
    public static readonly CaptureItemStatus Unrecognized = new(9, nameof(Unrecognized));

    /// <summary>Duplicata de artefato já processado, com ponteiro para o item original.</summary>
    public static readonly CaptureItemStatus Discarded = new(10, nameof(Discarded), isTerminal: true);

    /// <summary>Estado final: nenhuma transição sai daqui.</summary>
    public bool IsTerminal { get; }

    /// <summary>
    /// Se o read model pode projetar valor, beneficiário e linha digitável para o dono da fonte.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É regra de domínio, não de UI</strong> (ADR-008). Só dois estados expõem:
    /// <see cref="Promoted"/>, porque o boleto é do próprio tenant, e <see cref="Unrouted"/>,
    /// porque sem valor e beneficiário o usuário não tem como decidir se reivindica.
    /// </para>
    /// <para>
    /// <see cref="ForeignPayer"/> não expõe porque o sistema <em>sabe</em> que não é dele —
    /// mostrar seria vazamento gratuito. E os estados do funil também não expõem: antes do
    /// roteamento ninguém sabe de quem é o documento, e projetar ali vazaria durante a janela
    /// que antecede exatamente a descoberta de que o pagador é outro.
    /// </para>
    /// </remarks>
    public bool ExposesFinancialDetail { get; }

    private CaptureItemStatus(int id, string name, bool isTerminal = false, bool exposesFinancialDetail = false)
        : base(id, name)
    {
        IsTerminal = isTerminal;
        ExposesFinancialDetail = exposesFinancialDetail;
    }

    public bool CanTransitionTo(CaptureItemStatus target)
    {
        if (target is null || IsTerminal)
            return false;

        return (this, target) switch
        {
            // LinkFailed a partir de Received cobre o anexo que nao veio: o item existe, o
            // artefato nao. Sem isso, falha de download nao teria estado para onde ir.
            _ when this == Received && (target == Parsed || target == Locked || target == LinkPending
                || target == LinkFailed || target == Unrecognized || target == Discarded) => true,

            _ when this == LinkPending && (target == Parsed || target == LinkFailed
                || target == Unrecognized || target == Discarded) => true,

            // LinkFailed volta para LinkPending numa nova tentativa, e vai direto a Parsed quando
            // o humano resolve o download à mão — que é o desfecho previsto pelo degrau 5 do doc 09.
            _ when this == LinkFailed && (target == LinkPending || target == Parsed || target == Discarded) => true,

            _ when this == Locked && (target == Parsed || target == Unrouted || target == Discarded) => true,

            _ when this == Parsed && (target == Promoted || target == ForeignPayer
                || target == Unrouted || target == Discarded) => true,

            // Unrouted → Promoted é a reivindicação; → ForeignPayer é uma revalidação que
            // finalmente identificou o pagador e concluiu que não é deste tenant.
            _ when this == Unrouted && (target == Promoted || target == ForeignPayer || target == Discarded) => true,

            // O humano informa a linha digitável à mão sobre o que o parser não reconheceu.
            // → Received é a reabertura: a cascata mudou (prompt novo, modelo novo, degrau novo)
            // e vale reavaliar o mesmo artefato. Sem isto, o desfecho de um item ficaria congelado
            // no estado da cascata do dia em que ele passou.
            _ when this == Unrecognized && (target == Parsed || target == Received || target == Discarded) => true,

            // Locked reabre pelo mesmo motivo, e por mais um: o cadastro pode ter ganho o
            // documento que deriva a senha depois que o item já tinha sido triado.
            _ when this == Locked && target == Received => true,

            // LinkFailed reabre porque a nova tentativa de download é decisão de quem opera —
            // é o que faz o item voltar à fila sem alguém mexer no banco.
            _ when this == LinkFailed && target == Received => true,

            _ => false,
        };
    }
}
