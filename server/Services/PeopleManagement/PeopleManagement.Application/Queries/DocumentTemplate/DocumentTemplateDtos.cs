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
            public int? DocumentValidityDurationInDays { get; init; }
            public int? WorkloadInHours { get; init; }
            public bool AcceptsSignature { get; init; }
            public bool UsePreviousPeriod { get; init; }
            public DocumentGroupDocumentTemplateDto DocumentGroup { get; init; } = DocumentGroupDocumentTemplateDto.Empty;
            public TemplateFileInfoDto TemplateFileInfo { get; init; } =  TemplateFileInfoDto.Empty;
            public PoliciesDto Policies { get; init; } = PoliciesDto.Empty;

        }

        /// <summary>
        /// Regras ativas do template. Bloco nulo = regra não se aplica — mesma semântica do Composite do domínio
        /// e do contrato de escrita. A assinatura é uma regra como as outras: bloco presente = aceita assinatura,
        /// e ele carrega os locais (mesma forma da SignaturePolicy no domínio). Assim o retorno é uniforme — nada
        /// de assinatura solta no topo ou aninhada no arquivo.
        /// </summary>
        public record PoliciesDto
        {
            public ExpirationPolicyDto? Expiration { get; init; }
            public WorkloadPolicyDto? Workload { get; init; }
            public PeriodPolicyDto? Period { get; init; }
            public SignaturePolicyDto? Signature { get; init; }
            public static PoliciesDto Empty => new();
        }

        /// <summary>
        /// Assinatura como regra: presença = aceita assinatura. Carrega os locais (pode ser lista vazia — aceita
        /// assinatura sem posição fixa).
        /// </summary>
        public record SignaturePolicyDto
        {
            public PlaceSignatureDto[] PlaceSignatures { get; init; } = [];
        }

        public record ExpirationPolicyDto
        {
            public double DurationInDays { get; init; }

            /// <summary>Quantas vezes o documento renova antes de parar; null = renovação indefinida.</summary>
            public int? MaxRenewals { get; init; }
        }

        public record WorkloadPolicyDto
        {
            public double Hours { get; init; }
        }

        public record PeriodPolicyDto
        {
            public int PeriodTypeId { get; init; }
            public bool UsePreviousPeriod { get; init; }
        }

        public record DocumentGroupDocumentTemplateDto
        {
            public Guid Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public static DocumentGroupDocumentTemplateDto Empty => new();
        }

        public record TemplateFileInfoDto
        {
            public string BodyFileName { get; init; } = string.Empty;
            public string HeaderFileName { get; init; } = string.Empty;
            public string FooterFileName { get; init; } = string.Empty;
            public EnumerationDto[] RecoversDataType { get; init; } = [];
            public static TemplateFileInfoDto Empty => new();
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
