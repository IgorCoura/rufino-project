using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.Document
{
    public class DocumentDtos
    {
        public record DocumentSimpleDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
            public Guid EmployeeId { get; init; }
            public Guid CompanyId { get; init; }
            public Guid RequiredDocumentId { get; init; }
            public Guid DocumentTemplateId { get; init; }
            public bool UsePreviousPeriod { get; init; }
            public bool IsSignable { get; init; }
            public bool CanGenerateDocument { get; init; }
            public DateTime CreateAt { get; init; }
            public DateTime UpdateAt { get; init; }
        };

        public record DocumentDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public Guid EmployeeId { get; init; }
            public Guid CompanyId { get; init; }
            public Guid RequiredDocumentId { get; init; }
            public Guid DocumentTemplateId { get; init; }
            public bool UsePreviousPeriod { get; init; }
            public bool IsSignable { get; init; }
            public bool CanGenerateDocument { get; init; }
            public List<DocumentUnitDto> DocumentsUnits { get; init; } = [];
            public int TotalUnitsCount { get; init; }

            /// <summary>
            /// Data sugerida para agendar o envio da próxima unidade para assinatura: o vencimento da cobertura
            /// atual, ou seja, a maior validade entre as unidades OK e A Vencer. Null quando não há cobertura
            /// vigente com validade futura — aí não há o que sugerir e o usuário escolhe livremente.
            /// </summary>
            public DateOnly? SuggestedSignatureScheduleDate { get; init; }
            public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
            public DateTime CreateAt { get; init; }
            public DateTime UpdateAt { get; init; }

        };

        public record DocumentUnitDto
        {
            public Guid Id { get; init; }
            public string Content { get; init; } = string.Empty;
            public DateOnly? Validity { get; init; }
            public string? Name { get; init; } = string.Empty;
            public string? Extension { get; init; } = string.Empty;
            public EnumerationDto Status { get; init; } = EnumerationDto.Empty;
            public DateOnly Date { get; init; }
            public PeriodDto? Period { get; init; }
            public DateTime CreateAt { get; init; }
            public DateTime UpdateAt { get; init; }

            /// <summary>Data do envio agendado para assinatura; null quando não há agendamento.</summary>
            public DateOnly? ScheduledSignatureSendOn { get; init; }

            /// <summary>
            /// A unidade que esta veio renovar; null quando não é uma renovação. O cliente usa para marcar a
            /// linha como "Renovação" — sem isso ela é indistinguível de uma pendência qualquer na tela.
            /// </summary>
            public Guid? ReplacesDocumentUnitId { get; init; }

            public static implicit operator DocumentUnitDto(DocumentUnit documentUnit)
            {
                return new DocumentUnitDto
                {
                    ScheduledSignatureSendOn = documentUnit.ScheduledSignature?.SendOn,
                    ReplacesDocumentUnitId = documentUnit.ReplacesDocumentUnitId,
                    Id = documentUnit.Id,
                    Content = documentUnit.Content,
                    Validity = documentUnit.Validity,
                    Name = documentUnit.Name?.Value,
                    Extension = documentUnit.Extension?.Name,
                    Status = (EnumerationDto)documentUnit.Status,
                    Date = documentUnit.Date,
                    Period = documentUnit.Period != null ? new PeriodDto
                    {
                        Type = (EnumerationDto)documentUnit.Period.Type,
                        Day = documentUnit.Period.Day,
                        Week = documentUnit.Period.Week,
                        Month = documentUnit.Period.Month,
                        Year = documentUnit.Period.Year
                    } : null,
                    CreateAt = documentUnit.CreatedAt,
                    UpdateAt = documentUnit.UpdatedAt
                };
            }
        }

        public record PeriodDto
        {
            public EnumerationDto Type { get; init; } = EnumerationDto.Empty;
            public int? Day { get; init; }
            public int? Week { get; init; }
            public int? Month { get; init; }
            public int Year { get; init; }
        }

        public record DocumentUnitParams
        {
            public int? StatusId { get; init; }
            public int PageSize { get; init; } = 10;
            public int PageNumber { get; init; } = 1;
        }

        public record DownloadRangeDocumentItem(Guid DocumentId, IEnumerable<Guid> DocumentUnitIds);

    }
}
