using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate
{
    public record CreateDocumentTemplateCommand(Guid CompanyId, string BodyFileName, string HeaderFileName,
            string FooterFileName, int RecoverDataType, TimeSpan? DocumentValidityDuration) : IRequest<CreateDocumentTemplateResponse>
    {
        public DocumentTemplate ToDocumentTemplate(Guid Id, string directoryName) => DocumentTemplate.Create(Id, CompanyId, directoryName, BodyFileName, 
            HeaderFileName, FooterFileName, RecoverDataType, DocumentValidityDuration);
    }
}
