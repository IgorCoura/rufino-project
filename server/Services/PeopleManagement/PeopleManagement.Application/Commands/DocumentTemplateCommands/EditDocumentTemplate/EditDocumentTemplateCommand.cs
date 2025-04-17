

using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.EditDocumentTemplate
{

    public record EditDocumentTemplateCommand(Guid Id, Guid CompanyId, string Name, string Description, string BodyFileName, string HeaderFileName,
            string FooterFileName, int RecoverDataType, double? DocumentValidityDurationInDays, double? WorkloadInHours, EditPlaceSignatureModel[] PlaceSignatures) : IRequest<EditDocumentTemplateResponse>
    {
    }

    public record EditDocumentTemplateModel(Guid Id, string Name, string Description, string BodyFileName, string HeaderFileName,
            string FooterFileName, int RecoverDataType, double? DocumentValidityDurationInDays, double? WorkloadInHours, EditPlaceSignatureModel[] PlaceSignatures)
    {
        public EditDocumentTemplateCommand ToCommand(Guid company) => new(Id, company, Name, Description, BodyFileName,
            HeaderFileName, FooterFileName, RecoverDataType, DocumentValidityDurationInDays, WorkloadInHours, PlaceSignatures);
    }

    public record EditPlaceSignatureModel(int Type, int Page, double RelativePositionBotton, double RelativePositionLeft, double RelativeSizeX, double RelativeSizeY)
    {
        public PlaceSignature ToPlaceSignature() => PlaceSignature.Create(Type, Page, RelativePositionBotton, RelativePositionLeft, RelativeSizeX, RelativeSizeY);
    }
}
