using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.DocumentTemplate
{
    public class DocumentTemplateDtos
    {
        public record DocumentTemplateSimpleDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public Guid CompanyId { get; init; }
        }
        public record DocumentTemplateDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public Guid CompanyId { get; init; }
            public string BodyFileName { get; init; } = string.Empty;
            public string HeaderFileName { get; init; } = string.Empty;
            public string FooterFileName { get; init; } = string.Empty;
            public EnumerationDto RecoverDataType { get; init; } = EnumerationDto.Empty;
            public int? DocumentValidityDurationInDays { get; init; }
            public int? WorkloadInHours { get; init; }
            public PlaceSignatureDto[] PlaceSignatures { get; init; } = [];
        }

        public record PlaceSignatureDto
        {
            public EnumerationDto TypeSignature { get; init; } = EnumerationDto.Empty;
            public double Page { get; init; }
            public double RelativePositionBotton { get; init; }
            public double RelativePositionLeft { get; init; }
            public double RelativeSizeX { get; init; }
            public double RelativeSizeY { get; init; }
        }
        


    }
}
