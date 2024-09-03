using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate
{
    public record CreateDocumentTemplateCommand(Guid CompanyId, string Name, string Description, string BodyFileName, string HeaderFileName,
            string FooterFileName, int RecoverDataType, TimeSpan? DocumentValidityDuration, TimeSpan? Workload, PlaceSignatureModel[] PlaceSignatures) : IRequest<CreateDocumentTemplateResponse>
    {
        public DocumentTemplate ToDocumentTemplate(Guid Id, string directoryName) => DocumentTemplate.Create(Id, Name, Description, CompanyId, directoryName, BodyFileName, 
            HeaderFileName, FooterFileName, RecoverDataType, DocumentValidityDuration, Workload, PlaceSignatures.Select(x => x.ToPlaceSignature()).ToList());
    }

    public record CreateDocumentTemplateModel(Guid CompanyId, string Name, string Description, string BodyFileName, string HeaderFileName,
            string FooterFileName, int RecoverDataType, TimeSpan? DocumentValidityDuration, TimeSpan? Workload, PlaceSignatureModel[] PlaceSignatures)
    {
        public CreateDocumentTemplateCommand ToCommand(Guid company) => new (company, Name, Description, BodyFileName,
            HeaderFileName, FooterFileName, RecoverDataType, DocumentValidityDuration, Workload, PlaceSignatures);
    }

    public record PlaceSignatureModel(int Type, int Page, int RelativePositionBotton, int RelativePositionLeft, int RelativeSizeX, int RelativeSizeY)
    {
        public PlaceSignature ToPlaceSignature() => PlaceSignature.Create(Type, Page, RelativePositionBotton, RelativePositionLeft, RelativeSizeX, RelativeSizeY);
    }
}
