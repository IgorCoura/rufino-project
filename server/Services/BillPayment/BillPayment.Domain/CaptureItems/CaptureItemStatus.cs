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
    public static readonly CaptureItemStatus Locked = new(3, nameof(Locked), exposesSourceUrlForReview: true, awaitsRescue: true);

    /// <summary>Há um link para o documento e o download ainda não aconteceu.</summary>
    public static readonly CaptureItemStatus LinkPending = new(4, nameof(LinkPending));

    /// <summary>A escada de resolução de link esgotou sem trazer o documento.</summary>
    public static readonly CaptureItemStatus LinkFailed = new(5, nameof(LinkFailed), exposesSourceUrlForReview: true, awaitsRescue: true);

    /// <summary>Roteou para este tenant e virou <c>Bill</c>.</summary>
    public static readonly CaptureItemStatus Promoted =
        new(6, nameof(Promoted), isTerminal: true, exposesFinancialDetail: true);

    /// <summary>O pagador foi identificado e <strong>não</strong> é deste tenant.</summary>
    /// <remarks>
    /// <strong>Não é mais produzido desde 2026-08-28.</strong> O documento de outro pagador passou
    /// a ser descartado no processamento — sem item, sem arquivo, só a linha do livro-caixa —,
    /// pela regra de que um tenant nunca fica sabendo do boleto de outro. O estado continua
    /// existindo porque há linhas persistidas antes disso, e o portão de visibilidade que ele
    /// sustenta (<see cref="ExposesFinancialDetail"/> falso) precisa continuar valendo para elas.
    /// </remarks>
    public static readonly CaptureItemStatus ForeignPayer = new(7, nameof(ForeignPayer), isTerminal: true);

    /// <summary>Não foi possível determinar o dono. Fica na fila de reivindicação.</summary>
    public static readonly CaptureItemStatus Unrouted = new(8, nameof(Unrouted), exposesFinancialDetail: true, awaitsRescue: true);

    /// <summary>Nenhum boleto válido no artefato.</summary>
    public static readonly CaptureItemStatus Unrecognized = new(9, nameof(Unrecognized), exposesSourceUrlForReview: true, awaitsRescue: true);

    /// <summary>Duplicata de artefato já processado, com ponteiro para o item original.</summary>
    public static readonly CaptureItemStatus Discarded = new(10, nameof(Discarded), isTerminal: true);

    /// <summary>
    /// A cascata determinística não resolveu e o artefato espera a vez na fila do extrator de IA.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É o irmão de <see cref="LinkPending"/></strong>, e existe pela mesma razão: um
    /// artefato que depende de recurso externo escasso sai do caminho principal em vez de segurá-lo.
    /// A cota de IA é limitada por minuto e por dia, e a chamada leva de 3 a 5 segundos — medido em
    /// 2026-08-26, quando 27% dos itens consumiam 86% do tempo de processamento e travavam a fila
    /// inteira, cujo item mediano leva 150 ms.
    /// </para>
    /// <para>
    /// <strong>Não expõe detalhe financeiro</strong>: é estado de passagem do funil, e antes do
    /// roteamento ninguém sabe de quem é o documento (ADR-008).
    /// </para>
    /// </remarks>
    public static readonly CaptureItemStatus VisionPending = new(11, nameof(VisionPending));

    /// <summary>
    /// O processamento não conseguiu chegar a desfecho, e repetir deixou de ser a resposta.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Descreve o worker, não o documento.</strong> Os outros desfechos dizem o que o
    /// artefato é — <see cref="Unrecognized"/> é "não achei boleto aqui", <see cref="LinkFailed"/>
    /// é "o provedor não entregou o arquivo". Este diz que a leitura estourou: ou por uma regra
    /// que vai recusar igual na milésima tentativa, ou por tentativas esgotadas.
    /// </para>
    /// <para>
    /// <strong>Existe porque a alternativa era o laço eterno.</strong> Até 2026-08-26 toda falha
    /// era tratada como transitória e o item voltava a <see cref="Received"/> para sempre —
    /// medido na caixa real: quatro itens somaram 1.709 tentativas do mesmo erro
    /// (<c>BLP.BIL15</c>, PDF com dois boletos de naturezas diferentes), cada um ocupando
    /// permanentemente uma das dez vagas do lote e empurrando a fila para trás.
    /// </para>
    /// <para>
    /// <strong>Não é terminal</strong>: sai para <see cref="Received"/> pelo mesmo
    /// <c>Reopen</c> da quarentena. Falha de processamento é justamente o que uma correção de
    /// código costuma resolver, e congelar o item impediria a reavaliação.
    /// </para>
    /// </remarks>
    public static readonly CaptureItemStatus Failed = new(12, nameof(Failed), exposesSourceUrlForReview: true, awaitsRescue: true);

    /// <summary>
    /// Uma pessoa olhou e disse que não reconhece — o item sai da fila de pendências.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Não é <see cref="Discarded"/>, e a diferença não é cosmética.</strong> Descartar é
    /// decisão do <em>sistema</em> (duplicata de conteúdo, com ponteiro para o item original);
    /// reprovar é decisão de uma <em>pessoa</em>, e precisa registrar quem e quando, como a
    /// reivindicação já faz. Misturá-las apagaria a distinção entre "o sistema já tinha isto" e
    /// "alguém disse que não é dela" — e a fila que este estado existe para esvaziar deixaria de
    /// ser um filtro simples.
    /// </para>
    /// <para>
    /// <strong>Reversível de propósito.</strong> Reprovar por engano uma conta real é a falha
    /// silenciosa que o ADR-014 combate, e é fácil de cometer decidindo só por remetente e
    /// assunto. Sai por <c>Reopen</c>, como <see cref="Failed"/> e a quarentena.
    /// </para>
    /// <para>
    /// <strong>Não expõe URL nem detalhe financeiro</strong>: a decisão já foi tomada, e o
    /// portão que se abriu para ela decidir fecha de novo depois.
    /// </para>
    /// </remarks>
    public static readonly CaptureItemStatus Dismissed = new(13, nameof(Dismissed));

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

    /// <summary>
    /// Se o endereço de onde o documento veio (ou viria) pode ser mostrado a quem opera.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É um portão mais largo que o <see cref="ExposesFinancialDetail"/>, e a diferença
    /// é o ponto.</strong> URL de boleto é credencial ao portador — quem a tem, tem o documento —,
    /// e por isso ela seguia o mesmo gate do valor e do beneficiário. Só que isso a escondia
    /// justamente dos estados de quarentena, onde o sistema <em>não conseguiu</em> buscar o
    /// documento e depende de uma pessoa ir buscá-lo à mão. Sem a URL, essa pessoa fica sabendo
    /// que existe uma cobrança e não tem como chegar nela.
    /// </para>
    /// <para>
    /// O recorte é o mesmo princípio do ADR-008 uma camada abaixo: <strong>mostrar a quem precisa
    /// concluir, esconder de quem já concluiu</strong>. <see cref="ForeignPayer"/> continua
    /// fechado — ali o sistema sabe que o documento é de outro pagador, e entregar o link seria
    /// vazamento gratuito. <see cref="Dismissed"/> também: a decisão já foi tomada.
    /// </para>
    /// </remarks>
    public bool ExposesSourceUrl { get; }

    /// <summary>
    /// O artefato chegou e o sistema não conseguiu concluir sozinho — alguém precisa agir.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É o que liga a captura à expectativa.</strong> Um item preso aqui é exatamente o
    /// segundo dos dois alertas do ADR-014 — "chegou e não consegui ler" —, e sem esta marca a
    /// <c>BillExpectation</c> não teria como saber que houve chegada: o item falhou <em>antes</em>
    /// da extração e não carrega beneficiário nem vencimento para casar com nada.
    /// </para>
    /// <para>
    /// <strong>Não é sinônimo de <see cref="ExposesSourceUrl"/></strong>, embora hoje o conjunto
    /// coincida quase todo: aquele decide o que a tela mostra, este decide se um fato saiu do
    /// agregado. <c>Dismissed</c> é a contraprova — a pessoa já agiu, então não aguarda resgate.
    /// </para>
    /// </remarks>
    public bool AwaitsRescue { get; }

    private CaptureItemStatus(
        int id,
        string name,
        bool isTerminal = false,
        bool exposesFinancialDetail = false,
        bool exposesSourceUrlForReview = false,
        bool awaitsRescue = false)
        : base(id, name)
    {
        IsTerminal = isTerminal;
        ExposesFinancialDetail = exposesFinancialDetail;
        AwaitsRescue = awaitsRescue;

        // Quem já expõe valor e beneficiário expõe a URL por consequência — não faria sentido
        // entregar a linha digitável e esconder o endereço de onde o documento veio.
        ExposesSourceUrl = exposesFinancialDetail || exposesSourceUrlForReview;
    }

    public bool CanTransitionTo(CaptureItemStatus target)
    {
        if (target is null || IsTerminal)
            return false;

        return (this, target) switch
        {
            // A falha de processamento alcança QUALQUER estado de passagem, porque ela descreve
            // o worker e não o documento: estourar pode acontecer no download, na cascata, na
            // escada de roteamento ou na gravação. Declará-la degrau a degrau deixaria buracos —
            // foi exatamente assim que `VisionPending -> LinkFailed` ficou de fora e transformou
            // uma falha de download num item preso para sempre na fila da IA.
            _ when target == Failed && this != Failed => true,

            // E sai de lá pelo mesmo caminho da quarentena: falha de processamento é o desfecho
            // que uma correção de código costuma resolver, e congelá-la impediria a reavaliação.
            _ when this == Failed && (target == Received || target == Discarded) => true,

            // Reprovar alcança todo estado que espera decisão humana — e SÓ eles. Item já
            // promovido a boleto ou já atribuído a outro pagador não se reprova: no primeiro o
            // dinheiro já está em jogo, no segundo a decisão não é deste tenant.
            _ when target == Dismissed && (this == Unrecognized || this == Locked
                || this == LinkFailed || this == Failed || this == Unrouted) => true,

            // E desfaz: reprovar por engano uma conta real é a falha silenciosa que o ADR-014
            // combate, e decidir só por remetente e assunto erra com facilidade.
            _ when this == Dismissed && (target == Received || target == Discarded) => true,

            // LinkFailed a partir de Received cobre o anexo que nao veio: o item existe, o
            // artefato nao. Sem isso, falha de download nao teria estado para onde ir.
            _ when this == Received && (target == Parsed || target == Locked || target == LinkPending
                || target == LinkFailed || target == Unrecognized || target == Discarded
                || target == VisionPending) => true,

            // A fila de visão termina como o caminho normal terminaria: o que a IA resolver segue
            // para o roteamento (Parsed), e o que ela não resolver cai nos mesmos desfechos. Não
            // vai direto a Promoted nem a ForeignPayer — quem decide de quem é o boleto é a escada
            // de roteamento, que roda depois, e pular etapa aqui a contornaria.
            // LinkFailed entra aqui pelo mesmo motivo que entra a partir de Received: o anexo
            // pode não vir também nesta passagem, e o worker de visão rebaixa o artefato antes
            // do degrau 3. Faltava — e a falta transformava um download frustrado na fila da IA
            // numa transição inválida, item preso em VisionPending para sempre.
            _ when this == VisionPending && (target == Parsed || target == Unrecognized
                || target == Discarded || target == Locked || target == LinkFailed) => true,

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
