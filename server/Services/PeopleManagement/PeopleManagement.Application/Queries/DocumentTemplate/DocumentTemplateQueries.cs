using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Context;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static PeopleManagement.Application.Queries.Base.BaseDtos;
using static PeopleManagement.Application.Queries.DocumentTemplate.DocumentTemplateDtos;

namespace PeopleManagement.Application.Queries.DocumentTemplate
{
    public class DocumentTemplateQueries(PeopleManagementContext peopleManagementContext, ILocalStorageService localStorageService, DocumentTemplatesOptions documentTemplatesOptions) : IDocumentTemplateQueries
    {
        private PeopleManagementContext _context = peopleManagementContext;
        private ILocalStorageService _localStorageService = localStorageService;
        private DocumentTemplatesOptions _documentTemplatesOptions = documentTemplatesOptions;
        public async Task<IEnumerable<DocumentTemplateSimpleDto>> GetAllSimple(Guid companyId)
        {
            var query = _context.DocumentTemplates.AsNoTracking().Where(x => x.CompanyId == companyId);


            var result = await query.Select(x => new DocumentTemplateSimpleDto
            {
                Id = x.Id,
                Name = x.Name.Value,
                Description = x.Description.Value,
                CompanyId = x.CompanyId,
            }).ToArrayAsync();

            return result;

        }

        public async Task<DocumentTemplateDto> GetById(Guid companyId, Guid documentTemplateId)
        {

            var query =
                    from d in _context.DocumentTemplates
                    join g in _context.DocumentGroups on d.DocumentGroupId equals g.Id
                    where d.CompanyId == companyId && d.Id == documentTemplateId
                    select new
                    {
                        DocumentTemplates = d,
                        DocumentGroups = g
                    };

            var entity = await query.FirstOrDefaultAsync()
            ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), documentTemplateId.ToString()));

            var result =
                new DocumentTemplateDto
                {
                    Id = entity.DocumentTemplates.Id,
                    Name = entity.DocumentTemplates.Name.Value,
                    Description = entity.DocumentTemplates.Description.Value,
                    CompanyId = entity.DocumentTemplates.CompanyId,
                    DocumentValidityDurationInDays = entity.DocumentTemplates.DocumentValidityDuration.HasValue ? entity.DocumentTemplates.DocumentValidityDuration.Value.Days : null,
                    WorkloadInHours = entity.DocumentTemplates.Workload.HasValue ? entity.DocumentTemplates.Workload.Value.Hours : null,
                    UsePreviousPeriod = entity.DocumentTemplates.UsePreviousPeriod,
                    TemplateFileInfo = entity.DocumentTemplates.TemplateFileInfo == null ? new TemplateFileInfoDto() : new TemplateFileInfoDto
                    {
                        BodyFileName = entity.DocumentTemplates.TemplateFileInfo.BodyFileName.Value,
                        HeaderFileName = entity.DocumentTemplates.TemplateFileInfo.HeaderFileName.Value,
                        FooterFileName = entity.DocumentTemplates.TemplateFileInfo.FooterFileName.Value,
                        RecoversDataType = entity.DocumentTemplates.TemplateFileInfo.RecoversDataType.Select(x => (EnumerationDto)x).ToArray(),
                        PlaceSignatures = entity.DocumentTemplates.PlaceSignatures.Select(p => new PlaceSignatureDto
                        {
                            TypeSignature = new EnumerationDto
                            {
                                Id = p.Type.Id,
                                Name = p.Type.Name
                            },
                            Page = p.Page.Value,
                            RelativePositionBotton = p.RelativePositionBotton.Value,
                            RelativePositionLeft = p.RelativePositionLeft.Value,
                            RelativeSizeX = p.RelativeSizeX.Value,
                            RelativeSizeY = p.RelativeSizeY.Value,
                        }).ToArray(),
                    },
                    DocumentGroup = new DocumentGroupDocumentTemplateDto
                    {
                        Id = entity.DocumentGroups.Id,
                        Name = entity.DocumentGroups.Name.Value,
                        Description = entity.DocumentGroups.Description.Value,
                    }

                };


            return result;
        }

        public async Task<IEnumerable<DocumentTemplateDto>> GetAll(Guid companyId)
        {
            var query = await _context.DocumentTemplates.AsNoTracking().Where(x => x.CompanyId == companyId).ToArrayAsync();


            var result =  query.Select(x => new DocumentTemplateDto
            {
                Id = x.Id,
                Name = x.Name.Value,
                Description = x.Description.Value,
                CompanyId = x.CompanyId,
                DocumentValidityDurationInDays = x.DocumentValidityDuration.HasValue ? x.DocumentValidityDuration.Value.Days : null,
                WorkloadInHours = x.Workload.HasValue ? x.Workload.Value.Hours : null,
                UsePreviousPeriod = x.UsePreviousPeriod,
                TemplateFileInfo = x.TemplateFileInfo == null ? new TemplateFileInfoDto() : new TemplateFileInfoDto
                {
                    BodyFileName = x.TemplateFileInfo.BodyFileName.Value,
                    HeaderFileName = x.TemplateFileInfo.HeaderFileName.Value,
                    FooterFileName = x.TemplateFileInfo.FooterFileName.Value,
                    RecoversDataType = x.TemplateFileInfo.RecoversDataType.Select(x => (EnumerationDto)x).ToArray(),
                    PlaceSignatures = x.PlaceSignatures.Select(p => new PlaceSignatureDto
                    {
                        TypeSignature = new EnumerationDto
                        {
                            Id = p.Type.Id,
                            Name = p.Type.Name
                        },
                        Page = p.Page.Value,
                        RelativePositionBotton = p.RelativePositionBotton.Value,
                        RelativePositionLeft = p.RelativePositionLeft.Value,
                        RelativeSizeX = p.RelativeSizeX.Value,
                        RelativeSizeY = p.RelativeSizeY.Value,
                    }).ToArray(),
                }
            });

                                                                                                                                                                                               
            return result;

        }

        public async Task<bool> HasFile(Guid documentTemplateId, Guid companyId)
        {
            var documentTemplate = await _context.DocumentTemplates.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == documentTemplateId).FirstOrDefaultAsync() 
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), documentTemplateId.ToString()));

            if (documentTemplate.TemplateFileInfo == null)
                return false;

            var hasFile = await _localStorageService.HasFile(documentTemplate.TemplateFileInfo.Directory.Value, _documentTemplatesOptions.SourceDirectory);

            return hasFile;
        }

        public async Task<Stream> DownloadFile(Guid documentTemplateId, Guid companyId)
        {
            var documentTemplate = await _context.DocumentTemplates.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == documentTemplateId).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), documentTemplateId.ToString()));

            if (documentTemplate.TemplateFileInfo == null)
                return Stream.Null;

            var file =  await  _localStorageService.ZipDownloadAsync(documentTemplate.TemplateFileInfo.Directory.Value, _documentTemplatesOptions.SourceDirectory);

            return file;
        }

        public Task<List<EnumerationDto>> GetAllEvents()
        {
            var employeeEvents = EmployeeEvent.GetAll().Select(x => {
                if(x == null)
                    return null;
                return new EnumerationDto { Id = x.Id, Name = x.Name, };
            });

            var recurringEvents = RecurringEvents.GetAll().Select(x =>
            {
                if (x == null)
                    return null;
                return new EnumerationDto { Id = x.Id, Name = x.Name, };
            });

            var allEvents = employeeEvents.Concat(recurringEvents).Where(x => x != null).Select(x => (EnumerationDto)x!).ToList() ?? [];

            return Task.FromResult(allEvents);
        }

    }
}
