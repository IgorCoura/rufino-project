using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate
{
    public record CreateDocumentTemplateCommand(Guid CompanyId, string Name, string Description, string BodyFileName, string HeaderFileName,
            string FooterFileName, int RecoverDataType, double? DocumentValidityDurationInDays, double? WorkloadInHours, PlaceSignatureModel[] PlaceSignatures) : IRequest<CreateDocumentTemplateResponse>
    {
        public DocumentTemplate ToDocumentTemplate(Guid Id, string directoryName) => DocumentTemplate.Create(Id, Name, Description, CompanyId, directoryName, BodyFileName, 
            HeaderFileName, FooterFileName, RecoverDataType, DocumentValidityDurationInDays == null ? null : TimeSpan.FromDays((double)DocumentValidityDurationInDays), WorkloadInHours == null ? null: TimeSpan.FromHours((double)WorkloadInHours), PlaceSignatures.Select(x => x.ToPlaceSignature()).ToList());
    }

    public record CreateDocumentTemplateModel(string Name, string Description, string BodyFileName, string HeaderFileName,
            string FooterFileName, int RecoverDataType, double? DocumentValidityDurationInDays, double? WorkloadInHours, PlaceSignatureModel[] PlaceSignatures)
    {
        public CreateDocumentTemplateCommand ToCommand(Guid company) => new (company, Name, Description, BodyFileName,
            HeaderFileName, FooterFileName, RecoverDataType, DocumentValidityDurationInDays, WorkloadInHours, PlaceSignatures);
    }

    public record PlaceSignatureModel(int Type, int Page, double RelativePositionBotton, double RelativePositionLeft, double RelativeSizeX, double RelativeSizeY)
    {
        public PlaceSignature ToPlaceSignature() => PlaceSignature.Create(Type, Page, RelativePositionBotton, RelativePositionLeft, RelativeSizeX, RelativeSizeY);
    }
}
