using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces
{
    /// <summary>
    /// Conteúdo montado para uma unidade de documento, junto dos tipos de dado que não puderam ser recuperados.
    ///
    /// Um tipo que falhou é pulado (o comportamento histórico), então o conteúdo sai incompleto. Quem grava não se
    /// importa; quem compara sim — um bloco ausente por falha transitória é indistinguível de um dado que mudou.
    /// </summary>
    public sealed record DocumentContentResult(string Content, IReadOnlyCollection<RecoverDataType> FailedTypes)
    {
        public bool IsComplete => FailedTypes.Count == 0;
    }

    /// <summary>
    /// Monta o snapshot de dados (empresa, funcionário, cargo, …) que é gravado no <c>Content</c> da unidade e
    /// depois vira o PDF.
    ///
    /// É a ÚNICA definição do formato: tanto a gravação (<c>UpdateDocumentUnitDetails</c>) quanto a verificação de
    /// desatualização passam por aqui, para que a comparação seja feita contra uma string produzida exatamente do
    /// mesmo jeito.
    /// </summary>
    public interface IDocumentContentBuilder
    {
        /// <summary>
        /// Recupera os dados de [recoverDataTypes] para o funcionário e monta o JSON do conteúdo.
        ///
        /// [date], [validity] e [workloadEndDate] compõem o bloco de informações complementares. São valores, não
        /// leitura do template: quem chama decide se está gravando (valores recém-calculados) ou comparando
        /// (valores já gravados na unidade).
        /// </summary>
        Task<DocumentContentResult> Build(IEnumerable<RecoverDataType> recoverDataTypes, Guid employeeId, Guid companyId,
            DateOnly date, DateOnly? validity, DateOnly? workloadEndDate, CancellationToken cancellationToken = default);
    }
}
