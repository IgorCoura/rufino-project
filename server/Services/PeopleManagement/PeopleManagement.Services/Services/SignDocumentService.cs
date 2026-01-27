using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using System.Text.Json.Nodes;
using Document = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Document;
using Employee = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Employee;

namespace PeopleManagement.Services.Services
{
    public class SignDocumentService(ISignService signDocumentService, IDocumentRepository documentRepository, ICompanyRepository companyRepository, 
        IEmployeeRepository employeeRepository, IDocumentTemplateRepository documentTemplateRepository, IBlobService blobService, IDocumentService documentService) : ISignDocumentService
    {
        private readonly ISignService _signDocumentService = signDocumentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly ICompanyRepository _companyRepository = companyRepository;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        private readonly IDocumentTemplateRepository _documentTemplateRepository = documentTemplateRepository;
        private readonly IBlobService _blobService = blobService;
        private readonly IDocumentService _documentService = documentService;

        public async Task<Guid> GenerateDocumentToSign(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, DateTime dateLimitToSign, 
            int eminderEveryNDays, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId && x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
              ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            if (document.IsAwaitingSignatureDocumentUnit(documentUnitId))
                throw new DomainException(this, DomainErrors.Document.DocumentAlreadySentToSignature(documentUnitId));

            if (document.IsPendingDocumentUnit(documentUnitId) == false)
                throw new DomainException(this, DomainErrors.Document.IsNotPending());

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == document.DocumentTemplateId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            if(documentTemplate.TemplateFileInfo is null)
                throw new DomainException(this, DomainErrors.Document.DocumentNotHaveTemplate(documentId));

            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == employeeId && x.CompanyId == companyId, cancellation: cancellationToken)
               ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), employeeId.ToString()));

            var company = await _companyRepository.FirstOrDefaultAsync(x => x.Id == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Company), companyId.ToString()));

            var documentBytes = await _documentService.GeneratePdf(documentUnitId, documentId, employeeId, companyId, cancellationToken);

            using MemoryStream stream = new(documentBytes); 

            await SendToSignature(stream, documentUnitId, document, company, employee, documentTemplate.PlaceSignatures.ToArray(), dateLimitToSign, eminderEveryNDays, cancellationToken);
            
            document.MarkAsAwaitingDocumentUnitSignature(documentUnitId);

            return documentUnitId;                                                     
        }

        public async Task<Guid> InsertDocumentToSign(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId, Domain.AggregatesModel.DocumentAggregate.Extension extension, Stream stream,
            DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default)
        {
            var document = await _documentRepository.FirstOrDefaultAsync(x => x.Id == documentId && x.EmployeeId == employeeId
            && x.CompanyId == companyId, include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), documentId.ToString()));

            if(document.IsPendingDocumentUnit(documentUnitId) == false)
                throw new DomainException(this, DomainErrors.Document.IsNotPending());

            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == employeeId && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), employeeId.ToString()));

            var company = await _companyRepository.FirstOrDefaultAsync(x => x.Id == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Company), companyId.ToString()));

            var documentTemplate = await _documentTemplateRepository.FirstOrDefaultAsync(x => x.Id == document.DocumentTemplateId 
            && x.CompanyId == companyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), document.DocumentTemplateId.ToString()));

            await SendToSignature(stream, documentUnitId, document, company, employee, 
                documentTemplate.PlaceSignatures.ToArray() ?? [], dateLimitToSign, eminderEveryNDays, cancellationToken);

            document.MarkAsAwaitingDocumentUnitSignature(documentUnitId);

            return documentUnitId;
        }

        public async Task<Guid> InsertDocumentSigned(JsonNode contentBody, CancellationToken cancellationToken = default)
        {
            var docSigned = await _signDocumentService.GetFileFromDocSignedEvent(contentBody, cancellationToken);

            if (docSigned == null)
                return Guid.Empty;

            var document = await _documentRepository.FirstOrDefaultAsync(x => x.DocumentsUnits.Any(x => x.Id == docSigned.DocumentUnitId), include: x => x.Include(y => y.DocumentsUnits), cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentUnit), docSigned.DocumentUnitId.ToString()));

            if (document.IsAwaitingSignatureDocumentUnit(docSigned.DocumentUnitId) == false)
                return Guid.Empty;


            string fileNameWithExtesion = document.InsertUnitWithoutRequireValidation(docSigned.DocumentUnitId, Guid.NewGuid().ToString(), docSigned.Extension);

            await _blobService.UploadAsync(docSigned.DocStream, fileNameWithExtesion, document.CompanyId.ToString(), overwrite: false, cancellationToken: cancellationToken);

            return docSigned.DocumentUnitId;
        }

        private async Task SendToSignature(Stream stream, Guid documentUnitId, Document document, Company company,
            Employee employee, PlaceSignature[] placeSignatures, DateTime dateLimitToSign, int eminderEveryNDays, CancellationToken cancellationToken = default)
        {
            var documentSigningOptions = employee.DocumentSigningOptions;
            if (documentSigningOptions == DocumentSigningOptions.DigitalSignatureAndWhatsapp)
            {
                await _signDocumentService.SendToSignatureWithWhatsapp(stream, documentUnitId, document, company, employee,
                placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);
                return;
            }

            if(documentSigningOptions == DocumentSigningOptions.DigitalSignatureAndSelfie)
            {
                await _signDocumentService.SendToSignatureWithSelfie(stream, documentUnitId, document, company, employee,
                placeSignatures, dateLimitToSign, eminderEveryNDays, cancellationToken);
                return;
            }

            throw new DomainException(this, DomainErrors.Employee.InvalidDocumentDigitalSigningOptions(employee.Id));
        }

     
    }
}
