using static PeopleManagement.Application.Queries.Base.BaseDtos;
using static PeopleManagement.Application.Queries.Document.DocumentDtos;

namespace PeopleManagement.Application.Queries.DocumentDashboard
{
    public class DocumentDashboardDtos
    {
        /// <summary>
        /// Situação operacional de uma unidade de documento sob a ótica do RH.
        /// Cada bucket é um predicado exclusivo aplicado pelas queries do dashboard —
        /// a contagem do summary e a lista de unidades usam exatamente o mesmo filtro.
        /// </summary>
        public enum DashboardBucket
        {
            /// <summary>Vencidos: Deprecated/Invalid não substituídos, ou OK/Warning com validade no passado.</summary>
            Expired = 1,
            /// <summary>A vencer: OK/Warning com validade dentro do horizonte informado.</summary>
            Expiring = 2,
            /// <summary>Pendentes de entrega.</summary>
            Pending = 3,
            /// <summary>Aguardando assinatura digital.</summary>
            AwaitingSignature = 4,
            /// <summary>Aguardando validação do RH.</summary>
            RequiresValidation = 5,
        }

        public record DashboardFilterParams
        {
            /// <summary>Horizonte, em dias a partir de hoje, do bucket "a vencer".</summary>
            public int ExpiringInDays { get; init; } = 30;
            public int? EmployeeStatusId { get; init; }
            public string? EmployeeName { get; init; }
            public Guid? DocumentGroupId { get; init; }
            public Guid? DocumentTemplateId { get; init; }
        }

        public record DashboardUnitsParams : DashboardFilterParams
        {
            public DashboardBucket Bucket { get; init; } = DashboardBucket.Expired;
            public int PageSize { get; init; } = 50;
            public int PageNumber { get; init; } = 1;
        }

        public record DashboardSummaryDto
        {
            public int Expired { get; init; }
            public int Expiring { get; init; }
            public int Pending { get; init; }
            public int AwaitingSignature { get; init; }
            public int RequiresValidation { get; init; }
        }

        public record DashboardUnitDto
        {
            public Guid DocumentUnitId { get; init; }
            public Guid DocumentId { get; init; }
            public Guid EmployeeId { get; init; }
            public string EmployeeName { get; init; } = string.Empty;
            public EnumerationDto EmployeeStatus { get; init; } = EnumerationDto.Empty;
            public string DocumentTemplateName { get; init; } = string.Empty;
            public string DocumentGroupName { get; init; } = string.Empty;
            public DateOnly Date { get; init; }
            public DateOnly? Validity { get; init; }
            public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
            public PeriodDto? Period { get; init; }
            public bool HasFile { get; init; }
        }

        public record DashboardUnitsResult
        {
            public IEnumerable<DashboardUnitDto> Items { get; init; } = [];
            public int TotalCount { get; init; }
        }
    }
}
