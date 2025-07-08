using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate
{
    public sealed class CreateDocumentTemplateCommandHandler(IDocumentTemplateRepository documentTemplateRepository) : IRequestHandler<CreateDocumentTemplateCommand, CreateDocumentTemplateResponse>
    {
        private readonly IDocumentTemplateRepository _documentTemplateRepository = documentTemplateRepository;

        public async Task<CreateDocumentTemplateResponse> Handle(CreateDocumentTemplateCommand request, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            var directoryName = Guid.NewGuid().ToString();
            var documentTemplate = request.ToDocumentTemplate(id, directoryName);

            var result = await _documentTemplateRepository.InsertAsync(documentTemplate, cancellationToken);

            await _documentTemplateRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return result.Id;
        } 
    }

    public class CreateDocumentTemplateIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<CreateDocumentTemplateCommand, CreateDocumentTemplateResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<CreateDocumentTemplateCommand, CreateDocumentTemplateResponse>(mediator, logger, requestManager)
    {
        protected override CreateDocumentTemplateResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
