using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate
{
    public record CreateDocumentTemplateCommand(Guid CompanyId, string Name, string Description, double? DocumentValidityDurationInDays, double? WorkloadInHours, 
        TemplateFileInfoModel TemplateFileInfo) : IRequest<CreateDocumentTemplateResponse>
    {
        public DocumentTemplate ToDocumentTemplate(Guid Id, string directoryName) => DocumentTemplate.Create(Id, Name, Description, CompanyId, 
            DocumentValidityDurationInDays == null ? null : TimeSpan.FromDays((double)DocumentValidityDurationInDays),
            WorkloadInHours == null ? null: TimeSpan.FromHours((double)WorkloadInHours), TemplateFileInfo.ToTemplateFileInfo(directoryName));
    }

    public record CreateDocumentTemplateModel(string Name, string Description, double? DocumentValidityDurationInDays, double? WorkloadInHours, 
        TemplateFileInfoModel TemplateFileInfo)
    {
        public CreateDocumentTemplateCommand ToCommand(Guid company) => new (company, Name, Description, DocumentValidityDurationInDays, WorkloadInHours, TemplateFileInfo);
    }

    public record TemplateFileInfoModel(string BodyFileName, string HeaderFileName, string FooterFileName, int RecoverDataType, PlaceSignatureModel[] PlaceSignatures)
    {
        public TemplateFileInfo ToTemplateFileInfo(string directoryName) => TemplateFileInfo.Create(directoryName, BodyFileName, HeaderFileName, FooterFileName, 
            PlaceSignatures.Select(x => x.ToPlaceSignature()).ToList(), RecoverDataType);
    }

    public record PlaceSignatureModel(int Type, int Page, double RelativePositionBotton, double RelativePositionLeft, double RelativeSizeX, double RelativeSizeY)
    {
        public PlaceSignature ToPlaceSignature() => PlaceSignature.Create(Type, Page, RelativePositionBotton, RelativePositionLeft, RelativeSizeX, RelativeSizeY);
    }
}
