using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate.Interfaces;

namespace PeopleManagement.Application.Commands.RequireSecurityDocumentsCommands.CreateRequireSecurityDocuments
{
    public sealed class CreateRequireSecurityDocumentsCommandHandler(IRequireSecurityDocumentsRepository requireSecurityDocumentsRepository) : IRequestHandler<CreateRequireSecurityDocumentsCommand, CreateRequireSecurityDocumentsResponse>
    {
        private readonly IRequireSecurityDocumentsRepository _requireSecurityDocumentsRepository = requireSecurityDocumentsRepository;
        public async Task<CreateRequireSecurityDocumentsResponse> Handle(CreateRequireSecurityDocumentsCommand request, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            var requireSecurityDocuments = request.ToRequireSecurityDocuments(id);
            var result = await _requireSecurityDocumentsRepository.InsertAsync(requireSecurityDocuments, cancellationToken);
            await _requireSecurityDocumentsRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return result.Id;
        }

        public class CreateRequireSecurityDocumentsIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<CreateRequireSecurityDocumentsCommand, CreateRequireSecurityDocumentsResponse>> logger) : IdentifiedCommandHandler<CreateRequireSecurityDocumentsCommand, CreateRequireSecurityDocumentsResponse>(mediator, logger)
        {
            protected override CreateRequireSecurityDocumentsResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

        }
    }
}
