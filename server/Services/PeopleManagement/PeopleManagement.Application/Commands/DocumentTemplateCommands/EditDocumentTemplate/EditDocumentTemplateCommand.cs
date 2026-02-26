

using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.EditDocumentTemplate
{

    public record EditDocumentTemplateCommand(Guid Id, Guid CompanyId, string Name, string Description, EditTemplateFileInfoModel? TemplateFileInfo, double? DocumentValidityDurationInDays, double? WorkloadInHours, bool AcceptsSignature, EditPlaceSignatureModel[] PlaceSignatures, Guid DocumentGroupId, bool UsePreviousPeriod = false) : IRequest<EditDocumentTemplateResponse>
    {
    }

    public record EditDocumentTemplateModel(Guid Id, string Name, string Description, EditTemplateFileInfoModel? TemplateFileInfo, double? DocumentValidityDurationInDays, double? WorkloadInHours, bool AcceptsSignature, EditPlaceSignatureModel[] PlaceSignatures, Guid DocumentGroupId, bool UsePreviousPeriod = false)
    {
        public EditDocumentTemplateCommand ToCommand(Guid company) => new(Id, company, Name, Description, TemplateFileInfo, DocumentValidityDurationInDays, WorkloadInHours, AcceptsSignature, PlaceSignatures, DocumentGroupId, UsePreviousPeriod);
    }
    public record EditTemplateFileInfoModel(string BodyFileName, string HeaderFileName, string FooterFileName, int[] RecoversDataType)
    {
        public TemplateFileInfo ToTemplateFileInfo(string directoryName) => TemplateFileInfo.Create(directoryName, BodyFileName, HeaderFileName, FooterFileName,
            RecoversDataType.Select(x => RecoverDataType.FromValue<RecoverDataType>(x)).ToList());
    }
    public record EditPlaceSignatureModel(int Type, int Page, double RelativePositionBotton, double RelativePositionLeft, double RelativeSizeX, double RelativeSizeY)
    {
        public PlaceSignature ToPlaceSignature() => PlaceSignature.Create(Type, Page, RelativePositionBotton, RelativePositionLeft, RelativeSizeX, RelativeSizeY);
    }
}
