using static PeopleManagement.Application.Queries.Base.BaseDtos;
using static PeopleManagement.Application.Queries.Document.DocumentDtos;

namespace PeopleManagement.Application.Queries.BatchDocument
{
    public class BatchDocumentDtos
    {
        /// <summary>
        /// Filtros da tela de documentos em lote. Os tres eixos de escopo
        /// (grupo, template e funcionario) sao independentes: informar nenhum
        /// devolve todas as pendencias da empresa, e qualquer combinacao vale.
        /// </summary>
        public record BatchDocumentUnitParams
        {
            public Guid? DocumentGroupId { get; init; }
            public Guid? DocumentTemplateId { get; init; }
            public Guid? EmployeeId { get; init; }
            public int? EmployeeStatusId { get; init; }
            public string? EmployeeName { get; init; }
            public int? PeriodTypeId { get; init; }
            public int? PeriodYear { get; init; }
            public int? PeriodMonth { get; init; }
            public int? PeriodDay { get; init; }
            public int? PeriodWeek { get; init; }
            public int PageSize { get; init; } = 50;
            public int PageNumber { get; init; } = 1;
        }

        public record BatchDocumentUnitDto
        {
            public Guid DocumentUnitId { get; init; }
            public Guid DocumentId { get; init; }
            public Guid DocumentTemplateId { get; init; }
            public string DocumentTemplateName { get; init; } = string.Empty;
            public string DocumentGroupName { get; init; } = string.Empty;
            public Guid EmployeeId { get; init; }
            public string EmployeeName { get; init; } = string.Empty;
            public EnumerationDto EmployeeStatus { get; init; } = EnumerationDto.Empty;
            public DateOnly DocumentUnitDate { get; init; }
            public EnumerationDto DocumentUnitStatus { get; init; } = EnumerationDto.Empty;
            public PeriodDto? Period { get; init; }
            public bool IsSignable { get; init; }
            public bool CanGenerateDocument { get; init; }
        }

        public record BatchDocumentUnitsResult
        {
            public IEnumerable<BatchDocumentUnitDto> Items { get; init; } = [];
            public int TotalCount { get; init; }
        }

        /// <summary>
        /// Uma pendencia que falta criar: o par funcionario x template. Quando o
        /// filtro e por grupo, o mesmo funcionario aparece uma vez por template
        /// faltante — quem cria as unidades precisa saber de qual template.
        /// </summary>
        public record EmployeeMissingDocumentDto
        {
            public Guid EmployeeId { get; init; }
            public string EmployeeName { get; init; } = string.Empty;
            public EnumerationDto EmployeeStatus { get; init; } = EnumerationDto.Empty;
            public Guid DocumentTemplateId { get; init; }
            public string DocumentTemplateName { get; init; } = string.Empty;
        }
    }
}
