using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.Base.BaseDtos;
using static PeopleManagement.Application.Queries.DocumentTemplate.DocumentTemplateDtos;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;

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
            var query = _context.DocumentTemplates.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == documentTemplateId);


            var result = await query.Select(x => new DocumentTemplateDto { 
                Id = x.Id,
                Name = x.Name.Value,
                Description = x.Description.Value,
                CompanyId = x.CompanyId,
                DocumentValidityDurationInDays = x.DocumentValidityDuration.HasValue ? x.DocumentValidityDuration.Value.Days : null,
                WorkloadInHours = x.Workload.HasValue ? x.Workload.Value.Hours : null,
                TemplateFileInfo = x.TemplateFileInfo == null ? new TemplateFileInfoDto() : new TemplateFileInfoDto
                {
                    BodyFileName = x.TemplateFileInfo.BodyFileName.Value,
                    HeaderFileName = x.TemplateFileInfo.HeaderFileName.Value,
                    FooterFileName = x.TemplateFileInfo.FooterFileName.Value,
                    RecoverDataType = new EnumerationDto
                    {
                        Id = x.TemplateFileInfo.RecoverDataType.Id,
                        Name = x.TemplateFileInfo.RecoverDataType.Name
                    },
                    PlaceSignatures = x.TemplateFileInfo.PlaceSignatures.Select(p => new PlaceSignatureDto
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
            }).FirstOrDefaultAsync() 
            ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(DocumentTemplate), documentTemplateId.ToString()));

            return result;

        }
        public async Task<IEnumerable<DocumentTemplateDto>> GetAll(Guid companyId)
        {
            var query = _context.DocumentTemplates.AsNoTracking().Where(x => x.CompanyId == companyId);


            var result = await query.Select(x => new DocumentTemplateDto { 
                Id = x.Id,
                Name = x.Name.Value,
                Description = x.Description.Value,
                CompanyId = x.CompanyId,
                DocumentValidityDurationInDays = x.DocumentValidityDuration.HasValue ? x.DocumentValidityDuration.Value.Days : null,
                WorkloadInHours = x.Workload.HasValue ? x.Workload.Value.Hours : null,
                TemplateFileInfo = x.TemplateFileInfo == null ? new TemplateFileInfoDto() : new TemplateFileInfoDto
                {
                    BodyFileName = x.TemplateFileInfo.BodyFileName.Value,
                    HeaderFileName = x.TemplateFileInfo.HeaderFileName.Value,
                    FooterFileName = x.TemplateFileInfo.FooterFileName.Value,
                    RecoverDataType = new EnumerationDto
                    {
                        Id = x.TemplateFileInfo.RecoverDataType.Id,
                        Name = x.TemplateFileInfo.RecoverDataType.Name
                    },
                    PlaceSignatures = x.TemplateFileInfo.PlaceSignatures.Select(p => new PlaceSignatureDto
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
            }).ToArrayAsync();


                                                                                                                                                                                               
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

    }
}
