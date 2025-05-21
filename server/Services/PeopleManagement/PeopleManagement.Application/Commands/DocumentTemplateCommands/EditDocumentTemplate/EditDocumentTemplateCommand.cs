

using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.EditDocumentTemplate
{

    public record EditDocumentTemplateCommand(Guid Id, Guid CompanyId, string Name, string Description, EditTemplateFileInfoModel? TemplateFileInfo, double? DocumentValidityDurationInDays, double? WorkloadInHours, EditPlaceSignatureModel[] PlaceSignatures) : IRequest<EditDocumentTemplateResponse>
    {
    }

    public record EditDocumentTemplateModel(Guid Id, string Name, string Description, EditTemplateFileInfoModel? TemplateFileInfo, double? DocumentValidityDurationInDays, double? WorkloadInHours, EditPlaceSignatureModel[] PlaceSignatures)
    {
        public EditDocumentTemplateCommand ToCommand(Guid company) => new(Id, company, Name, Description, TemplateFileInfo, DocumentValidityDurationInDays, WorkloadInHours, PlaceSignatures);
    }
    public record EditTemplateFileInfoModel(string BodyFileName, string HeaderFileName, string FooterFileName, int RecoverDataType)
    {
        public TemplateFileInfo ToTemplateFileInfo(string directoryName) => TemplateFileInfo.Create(directoryName, BodyFileName, HeaderFileName, FooterFileName,
            RecoverDataType);
    }
    public record EditPlaceSignatureModel(int Type, int Page, double RelativePositionBotton, double RelativePositionLeft, double RelativeSizeX, double RelativeSizeY)
    {
        public PlaceSignature ToPlaceSignature() => PlaceSignature.Create(Type, Page, RelativePositionBotton, RelativePositionLeft, RelativeSizeX, RelativeSizeY);
    }
}
