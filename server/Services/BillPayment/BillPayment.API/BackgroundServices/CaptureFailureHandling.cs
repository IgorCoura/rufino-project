namespace BillPayment.API.BackgroundServices;

using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Queries.CaptureItems;
using BillPayment.Domain.SeedWork;

/// <summary>
/// O que fazer quando o processamento de um artefato estoura — compartilhado pelas duas faixas.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque tratar toda falha como passageira produziu laço eterno.</strong> Medido
/// em 2026-08-26: quatro artefatos somaram 1.709 tentativas do mesmo erro, cada um ocupando
/// permanentemente uma vaga do lote. As duas faixas precisam da mesma disciplina, e escrevê-la
/// duas vezes seria o começo de duas políticas de retry divergindo em silêncio.
/// </para>
/// </remarks>
internal static class CaptureFailureHandling
{
    /// <summary>
    /// A falha é uma recusa que vai se repetir igual, ou um tropeço que pode não se repetir?
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong><c>DomainException</c> é o domínio dizendo não</strong> — regra de negócio
    /// avaliando os mesmos bytes e chegando à mesma conclusão em toda tentativa. Foi o caso
    /// medido: <c>BLP.BIL15</c>, um PDF com dois boletos de naturezas diferentes, que nenhuma
    /// repetição jamais transformaria em boleto único.
    /// </para>
    /// <para>
    /// Todo o resto — rede, provedor fora do ar, balde indisponível, timeout — é tratado como
    /// passageiro e ganha as tentativas do <c>CaptureRetryOptions</c>. <strong>Errar para o lado
    /// de "passageiro" é o lado seguro</strong>: no máximo se gasta o teto de tentativas antes de
    /// desistir, ao passo que classificar rede instável como permanente jogaria na quarentena um
    /// boleto que a tentativa seguinte traria.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <strong>A indisponibilidade do extrator é a exceção à regra do <c>DomainException</c>.</strong>
    /// Ela é lançada como exceção de domínio por ser o único jeito de devolver o item à fila, mas
    /// o que ela descreve é a REDE, não os bytes: o provedor não respondeu, e a tentativa
    /// seguinte pode muito bem responder. Tratá-la como determinística mandaria para a quarentena
    /// exatamente os documentos que o 503 derrubou — que é o defeito que ela existe para corrigir.
    /// </remarks>
    public static bool IsPermanent(Exception failure)
        => failure is DomainException domain
            && !string.Equals(domain.Id, PROVIDER_UNAVAILABLE, StringComparison.Ordinal);

    /// <summary>O extrator de IA não respondeu — <c>ExtractionErrors.ProviderUnavailable</c>.</summary>
    private const string PROVIDER_UNAVAILABLE = "BLP.EXT08";

    /// <summary>
    /// Anota a falha no agregado, num escopo novo, e diz se o item desistiu.
    /// </summary>
    /// <remarks>
    /// <strong>O escopo tem de ser novo.</strong> O escopo onde o processamento estourou tem um
    /// <c>DbContext</c> com o agregado meio mutado — no caso medido o item já tinha passado por
    /// <c>MarkParsed</c> antes de a criação do boleto recusar. Gravar por cima disso persistiria
    /// um estado que nunca foi válido.
    /// </remarks>
    public static async Task<bool> RecordAsync(
        IServiceScopeFactory scopeFactory,
        PendingCaptureItem item,
        Exception failure,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(
                new RecordCaptureItemFailureCommand(
                    item.TenantId,
                    item.CaptureItemId,
                    Describe(failure),
                    IsPermanent(failure)),
                cancellationToken);

            return result.GaveUp;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Falhar ao registrar a falha não pode derrubar o worker. O item continua com o
            // aluguel vivo e volta quando ele vencer — pior que isso seria o ciclo inteiro parar.
            logger.LogError(ex, "Could not record the failure of capture item {ItemId}.", item.CaptureItemId);
            return false;
        }
    }

    /// <summary>A mensagem que vai para a coluna de diagnóstico — sem a pilha, que não cabe lá.</summary>
    private static string Describe(Exception failure)
        => failure is DomainException domain
            ? $"[{domain.Id}] {domain.Message}"
            : $"{failure.GetType().Name}: {failure.Message}";
}
