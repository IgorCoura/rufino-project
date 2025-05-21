using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;

namespace PeopleManagement.Application.Commands.RequireDocumentsCommands.EditRequireSecurityDocuments
{
    public sealed class EditRequireDocumentsCommandHandler(IRequireDocumentsRepository requireDocumentsRepository) : IRequestHandler<EditRequireDocumentsCommand, EditRequireDocumentsResponse>
    {
        private readonly IRequireDocumentsRepository _requireDocumentsRepository = requireDocumentsRepository;
        public async Task<EditRequireDocumentsResponse> Handle(EditRequireDocumentsCommand request, CancellationToken cancellationToken)
        {
            var requireDocument = await _requireDocumentsRepository.FirstOrDefaultAsync(x => x.Id == request.Id && x.CompanyId == request.CompanyId)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(RequireDocuments), request.Id.ToString()));

            requireDocument.Edit(request.AssociationId, request.AssociationType, request.Name, request.Description, 
                request.ListenEvents.Select(x => x.ToObjectValue()).ToList(), request.DocumentsTemplatesIds);
            await _requireDocumentsRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return requireDocument.Id;
        }

        public class CreateRequireSecurityDocumentsIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<EditRequireDocumentsCommand, EditRequireDocumentsResponse>> logger) : IdentifiedCommandHandler<EditRequireDocumentsCommand, EditRequireDocumentsResponse>(mediator, logger)
        {
            protected override EditRequireDocumentsResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

        }
    }
}
