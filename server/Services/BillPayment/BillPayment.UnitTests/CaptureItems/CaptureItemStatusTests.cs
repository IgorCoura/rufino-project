namespace BillPayment.UnitTests.CaptureItems;

using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.SeedWork;

public class CaptureItemStatusTests
{
    private static readonly string[] ExpostosEsperados = ["Promoted", "Unrouted"];

    // A projeção financeira da quarentena é decidida pelo status, e só Promoted e Unrouted expõem:
    // o primeiro porque o boleto é do tenant, o segundo porque sem valor não há como reivindicar.
    [Fact]
    public void ExposesFinancialDetail_ShouldBeTrueOnlyForPromotedAndUnrouted()
    {
        var expostos = Enumeration.GetAll<CaptureItemStatus>()
            .Where(s => s.ExposesFinancialDetail)
            .Select(s => s.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpostosEsperados, expostos);
    }

    // ForeignPayer nunca expõe conteúdo financeiro — o sistema SABE que não é do usuário (ADR-008).
    [Fact]
    public void ExposesFinancialDetail_ForForeignPayer_ShouldBeFalse()
        => Assert.False(CaptureItemStatus.ForeignPayer.ExposesFinancialDetail);

    // Os estados do funil não expõem: antes do roteamento ninguém sabe de quem é o documento.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(11)]
    public void ExposesFinancialDetail_ForPipelineStatuses_ShouldBeFalse(int statusId)
        => Assert.False(Enumeration.FromValue<CaptureItemStatus>(statusId).ExposesFinancialDetail);

    // Promoted, ForeignPayer e Discarded são terminais e não aceitam nenhuma transição de saída.
    [Theory]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(10)]
    public void CanTransitionTo_FromTerminalStatus_ShouldAlwaysBeFalse(int statusId)
    {
        var terminal = Enumeration.FromValue<CaptureItemStatus>(statusId);

        Assert.True(terminal.IsTerminal);
        Assert.All(
            Enumeration.GetAll<CaptureItemStatus>(),
            target => Assert.False(terminal.CanTransitionTo(target)));
    }

    // Transições válidas do funil e dos desfechos de quarentena.
    [Theory]
    [InlineData(1, 2)]   // Received     → Parsed
    [InlineData(1, 3)]   // Received     → Locked
    [InlineData(1, 4)]   // Received     → LinkPending
    [InlineData(1, 9)]   // Received     → Unrecognized
    [InlineData(4, 5)]   // LinkPending  → LinkFailed
    [InlineData(5, 4)]   // LinkFailed   → LinkPending (nova tentativa)
    [InlineData(5, 2)]   // LinkFailed   → Parsed (humano resolveu o download)
    [InlineData(3, 2)]   // Locked       → Parsed (senha derivada abriu)
    [InlineData(3, 8)]   // Locked       → Unrouted
    [InlineData(2, 6)]   // Parsed       → Promoted
    [InlineData(2, 7)]   // Parsed       → ForeignPayer
    [InlineData(2, 8)]   // Parsed       → Unrouted
    [InlineData(8, 6)]   // Unrouted     → Promoted (reivindicação)
    [InlineData(8, 7)]   // Unrouted     → ForeignPayer
    [InlineData(9, 2)]   // Unrecognized → Parsed (linha digitável informada à mão)
    [InlineData(1, 11)]  // Received      → VisionPending (cede a vez para a fila de IA)
    [InlineData(11, 2)]  // VisionPending → Parsed (a IA resolveu)
    [InlineData(11, 9)]  // VisionPending → Unrecognized (a IA não resolveu)
    [InlineData(11, 10)] // VisionPending → Discarded (remetente desconhecido)
    public void CanTransitionTo_WithValidPair_ShouldBeTrue(int fromId, int toId)
        => Assert.True(Transition(fromId, toId));

    // Transições que pulariam etapas ou reabririam um desfecho são recusadas.
    [Theory]
    [InlineData(1, 6)]   // Received     → Promoted (sem extração)
    [InlineData(1, 7)]   // Received     → ForeignPayer (sem extração)
    [InlineData(1, 8)]   // Received     → Unrouted (sem extração)
    [InlineData(2, 1)]   // Parsed       → Received (não se volta)
    [InlineData(11, 6)]  // VisionPending → Promoted (sem passar pelo roteamento)
    [InlineData(11, 7)]  // VisionPending → ForeignPayer (idem)
    [InlineData(2, 3)]   // Parsed       → Locked
    [InlineData(8, 2)]   // Unrouted     → Parsed
    [InlineData(9, 6)]   // Unrecognized → Promoted (sem instrumento válido)
    public void CanTransitionTo_WithInvalidPair_ShouldBeFalse(int fromId, int toId)
        => Assert.False(Transition(fromId, toId));

    // Alvo nulo nunca é transição válida.
    [Fact]
    public void CanTransitionTo_WithNullTarget_ShouldBeFalse()
        => Assert.False(CaptureItemStatus.Received.CanTransitionTo(null!));

    private static bool Transition(int fromId, int toId)
        => Enumeration.FromValue<CaptureItemStatus>(fromId)
            .CanTransitionTo(Enumeration.FromValue<CaptureItemStatus>(toId));
}
