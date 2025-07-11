﻿using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentSigned
{
    public class InsertDocumentSignedCommandHandler(ISignDocumentService signDocumentService, IDocumentRepository documentRepository) : IRequestHandler<InsertDocumentSignedCommand, InsertDocumentSignedResponse>
    {
        private readonly ISignDocumentService _signDocumentService = signDocumentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;

        public async Task<InsertDocumentSignedResponse> Handle(InsertDocumentSignedCommand request, CancellationToken cancellationToken)
        {
            var result = await _signDocumentService.InsertDocumentSigned(request.ContentBody, cancellationToken);
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
    public class ReceiveDocumentSignedIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<InsertDocumentSignedCommand, InsertDocumentSignedResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<InsertDocumentSignedCommand, InsertDocumentSignedResponse>(mediator, logger, requestManager)
    {
        protected override InsertDocumentSignedResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
