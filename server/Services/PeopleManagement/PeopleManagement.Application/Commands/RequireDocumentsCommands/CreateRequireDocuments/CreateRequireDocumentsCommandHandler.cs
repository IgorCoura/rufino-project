﻿using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;

namespace PeopleManagement.Application.Commands.RequireDocumentsCommands.CreateRequireSecurityDocuments
{
    public sealed class CreateRequireDocumentsCommandHandler(IRequireDocumentsRepository requireDocumentsRepository) : IRequestHandler<CreateRequireDocumentsCommand, CreateRequireDocumentsResponse>
    {
        private readonly IRequireDocumentsRepository _requireDocumentsRepository = requireDocumentsRepository;
        public async Task<CreateRequireDocumentsResponse> Handle(CreateRequireDocumentsCommand request, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            var requireSecurityDocuments = request.ToRequireSecurityDocuments(id);
            var result = await _requireDocumentsRepository.InsertAsync(requireSecurityDocuments, cancellationToken);
            await _requireDocumentsRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return result.Id;
        }

        public class CreateRequireSecurityDocumentsIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<CreateRequireDocumentsCommand, CreateRequireDocumentsResponse>> logger) : IdentifiedCommandHandler<CreateRequireDocumentsCommand, CreateRequireDocumentsResponse>(mediator, logger)
        {
            protected override CreateRequireDocumentsResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

        }
    }
}