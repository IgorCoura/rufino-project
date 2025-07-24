using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentGroupCommands.EditDocumentGroup
{
    public class EditDocumentGroupCommandHandler(IDocumentGroupRepository documentGroupRepository) : IRequestHandler<EditDocumentGroupCommand, EditDocumentGroupResponse>
    {
        private readonly IDocumentGroupRepository _documentGroupRepository = documentGroupRepository;
        public async Task<EditDocumentGroupResponse> Handle(EditDocumentGroupCommand request, CancellationToken cancellationToken)
        {
            var document = await _documentGroupRepository.FirstOrDefaultAsync(x => x.Id == request.Id && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentGroup), request.Id.ToString()));

            document.Edit(request.Name, request.Description);

            await _documentGroupRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return document.Id;
        }

    }

    public class EditDocumentGroupIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<EditDocumentGroupCommand, EditDocumentGroupResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<EditDocumentGroupCommand, EditDocumentGroupResponse>(mediator, logger, requestManager)
    {
        protected override EditDocumentGroupResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }

}
