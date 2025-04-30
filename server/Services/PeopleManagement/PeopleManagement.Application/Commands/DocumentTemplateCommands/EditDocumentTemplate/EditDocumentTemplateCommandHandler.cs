using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Application.Commands.Identified;

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

            documentTemplate.Name = request.Name;
            documentTemplate.Description = request.Description;
            documentTemplate.TemplateFileInfo.BodyFileName = request.BodyFileName;
            documentTemplate.TemplateFileInfo.HeaderFileName = request.HeaderFileName;
            documentTemplate.TemplateFileInfo.FooterFileName = request.FooterFileName;
            documentTemplate.TemplateFileInfo.RecoverDataType = request.RecoverDataType;
            documentTemplate.DocumentValidityDuration = request.DocumentValidityDurationInDays.HasValue ? TimeSpan.FromDays((double)request.DocumentValidityDurationInDays!) : null;
            documentTemplate.Workload = request.WorkloadInHours.HasValue ? TimeSpan.FromHours((double)request.WorkloadInHours!) : null; ;
            documentTemplate.TemplateFileInfo.PlaceSignatures = request.PlaceSignatures.Select(x => x.ToPlaceSignature()).ToList();

            await _documentTemplateRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return documentTemplate.Id;
        }
    }

    public class EditDocumentTemplateIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<EditDocumentTemplateCommand, EditDocumentTemplateResponse>> logger) : IdentifiedCommandHandler<EditDocumentTemplateCommand, EditDocumentTemplateResponse>(mediator, logger)
    {
        protected override EditDocumentTemplateResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }

}
