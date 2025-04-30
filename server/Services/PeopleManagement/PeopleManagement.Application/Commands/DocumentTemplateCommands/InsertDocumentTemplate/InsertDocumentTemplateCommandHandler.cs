
using PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Application.Commands.DocumentTemplateCommands.InsertDocumentTemplate
{
    public sealed class InsertDocumentTemplateCommandHandler(IDocumentTemplateRepository documentTemplateRepository, 
        ILocalStorageService localStorageService, DocumentTemplatesOptions documentTemplateOptions) : IRequestHandler<InsertDocumentTemplateCommand, InsertDocumentTemplateResponse>
    {
        private readonly IDocumentTemplateRepository _documentTemplateRepository = documentTemplateRepository;
        private readonly ILocalStorageService _localStorageService = localStorageService;
        private readonly DocumentTemplatesOptions _documentTemplateOptions = documentTemplateOptions;
        public async Task<InsertDocumentTemplateResponse> Handle(InsertDocumentTemplateCommand request, CancellationToken cancellationToken)
        {
            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == request.DocumentTemplateId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), request.DocumentTemplateId.ToString()));
            
            if(await _localStorageService.HasFile(documentTemplate.TemplateFileInfo.Directory.ToString(), _documentTemplateOptions.SourceDirectory))
            {
                await _localStorageService.DeleteAsync(documentTemplate.TemplateFileInfo.Directory.ToString(), _documentTemplateOptions.SourceDirectory);
            }

            await _localStorageService.UnzipUploadAsync(request.Stream, documentTemplate.TemplateFileInfo.Directory.ToString(), _documentTemplateOptions.SourceDirectory, cancellationToken);

            return documentTemplate.Id;
        }
    }

    public class InsertDocumentTemplateIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<InsertDocumentTemplateCommand, InsertDocumentTemplateResponse>> logger) : IdentifiedCommandHandler<InsertDocumentTemplateCommand, InsertDocumentTemplateResponse>(mediator, logger)
    {
        protected override InsertDocumentTemplateResponse CreateResultForDuplicateRequest() => new(Guid.Empty);

    }
}
