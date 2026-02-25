using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate
{
    public record CreateDocumentTemplateCommand(Guid CompanyId, string Name, string Description, double? DocumentValidityDurationInDays, double? WorkloadInHours, 
        TemplateFileInfoModel? TemplateFileInfo, PlaceSignatureModel[] PlaceSignatures, Guid DocumentGroupId, bool UsePreviousPeriod = false) : IRequest<CreateDocumentTemplateResponse>
    {
        public DocumentTemplate ToDocumentTemplate(Guid Id, string directoryName) => DocumentTemplate.Create(Id, 
            Name, 
            Description, 
            CompanyId, 
            DocumentValidityDurationInDays,
            WorkloadInHours, 
            TemplateFileInfo == null ? null : TemplateFileInfo!.ToTemplateFileInfo(directoryName), 
            PlaceSignatures.Select(x => x.ToPlaceSignature()).ToList(),
            DocumentGroupId,
            UsePreviousPeriod);
    }

    public record CreateDocumentTemplateModel(string Name, string Description, double? DocumentValidityDurationInDays, double? WorkloadInHours, 
        TemplateFileInfoModel? TemplateFileInfo, PlaceSignatureModel[] PlaceSignatures, Guid documentGroupId, bool UsePreviousPeriod = false)
    {
        public CreateDocumentTemplateCommand ToCommand(Guid company) => new (company, Name, Description, DocumentValidityDurationInDays, WorkloadInHours, TemplateFileInfo, PlaceSignatures, documentGroupId, UsePreviousPeriod);
    }

    public record TemplateFileInfoModel(string BodyFileName, string HeaderFileName, string FooterFileName, int[] RecoversDataType)
    {
        public TemplateFileInfo ToTemplateFileInfo(string directoryName) => TemplateFileInfo.Create(directoryName, BodyFileName, HeaderFileName, FooterFileName,
            RecoversDataType.Select(x => RecoverDataType.FromValue<RecoverDataType>(x)).ToList());
    }

    public record PlaceSignatureModel(int Type, int Page, double RelativePositionBotton, double RelativePositionLeft, double RelativeSizeX, double RelativeSizeY)
    {
        public PlaceSignature ToPlaceSignature() => PlaceSignature.Create(Type, Page, RelativePositionBotton, RelativePositionLeft, RelativeSizeX, RelativeSizeY);
    }
}
