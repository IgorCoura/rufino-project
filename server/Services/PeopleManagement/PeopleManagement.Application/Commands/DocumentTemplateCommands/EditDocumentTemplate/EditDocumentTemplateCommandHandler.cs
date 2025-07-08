using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.EditDocumentTemplate
{
    public class EditDocumentTemplateCommandHandler : IRequestHandler<EditDocumentTemplateCommand, EditDocumentTemplateResponse>
    {
        private readonly IDocumentTemplateRepository _documentTemplateRepository;
        public EditDocumentTemplateCommandHandler(IDocumentTemplateRepository documentTemplateRepository)
        {
            _documentTemplateRepository = documentTemplateRepository;
        }
        public async Task<EditDocumentTemplateResponse> Handle(EditDocumentTemplateCommand request, CancellationToken cancellationToken)
        {
            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == request.Id && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), request.Id.ToString()));


            documentTemplate.Edit(
                request.Name,
                request.Description,
                request.DocumentValidityDurationInDays,
                request.WorkloadInHours,
                request.TemplateFileInfo == null ? null : request.TemplateFileInfo?.ToTemplateFileInfo(documentTemplate.TemplateFileInfo?.Directory.Value ?? Guid.NewGuid().ToString()),
                request.PlaceSignatures.Select(x => x.ToPlaceSignature()).ToList()
                );


            await _documentTemplateRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return documentTemplate.Id;
        }
    }

    public class EditDocumentTemplateIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<EditDocumentTemplateCommand, EditDocumentTemplateResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<EditDocumentTemplateCommand, EditDocumentTemplateResponse>(mediator, logger, requestManager)
    {
        protected override EditDocumentTemplateResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }

}
