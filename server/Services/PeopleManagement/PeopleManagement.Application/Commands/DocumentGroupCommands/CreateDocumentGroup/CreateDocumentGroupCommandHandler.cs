
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentGroupCommands.CreateDocumentGroup
{
    public class CreateDocumentGroupCommandHandler(IDocumentGroupRepository documentGroupRepository) : IRequestHandler<CreateDocumentGroupCommand, CreateDocumentGroupResponse>
    {
        private readonly IDocumentGroupRepository _documentGroupRepository = documentGroupRepository;
        public async Task<CreateDocumentGroupResponse> Handle(CreateDocumentGroupCommand request, CancellationToken cancellationToken)
        {
            var documentGroup = DocumentGroup.Create(Guid.NewGuid(), request.Name, request.Description, request.CompanyId);
            await _documentGroupRepository.InsertAsync(documentGroup, cancellationToken);
            await _documentGroupRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return documentGroup.Id;
        }
    }

    public class CreateDocumentGroupIdentifiedCommandHandler(
        IMediator mediator, 
        ILogger<IdentifiedCommandHandler<CreateDocumentGroupCommand, CreateDocumentGroupResponse>> logger, 
        IRequestManager requestManager) 
        : IdentifiedCommandHandler<CreateDocumentGroupCommand, CreateDocumentGroupResponse>(mediator, logger, requestManager)
    {
        protected override CreateDocumentGroupResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
