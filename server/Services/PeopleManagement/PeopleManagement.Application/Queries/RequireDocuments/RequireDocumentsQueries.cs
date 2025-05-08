using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.Base.BaseDtos;
using static PeopleManagement.Application.Queries.RequireDocuments.RequireDocumentsDtos;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Application.Queries.RequireDocuments
{
    public class RequireDocumentsQueries(PeopleManagementContext peopleManagementContext) : IRequireDocumentsQueries
    {
        private PeopleManagementContext _context = peopleManagementContext;
        public async Task<IEnumerable<RequireDocumentSimpleDto>> GetAllSimple(Guid companyId)
        {
            var query = _context.RequireDocuments.AsNoTracking().Where(x => x.CompanyId == companyId);

            var result = await query.Select(x => new RequireDocumentSimpleDto
            {
                Id = x.Id,
                Name = x.Name.Value,
                Description = x.Description.Value,
                CompanyId = x.CompanyId,
            }).ToArrayAsync();

            return result;

        }

        public async Task<RequireDocumentDto> GetById(Guid requireDocumentId, Guid companyId)
        {
            var query = _context.RequireDocuments.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == requireDocumentId);
                      

            var result = await query.Select(x => new RequireDocumentDto
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                Name = x.Name.Value,
                Description = x.Description.Value,
                Association = x.AssociationId,
                AssociationType = new EnumerationDto
                {
                    Id= x.AssociationType.Id,
                    Name= x.AssociationType.Name,
                },
                DocumentsTemplates = _context.DocumentTemplates
                                .Where(t => x.DocumentsTemplatesIds.Contains(t.Id)).Select(d => new RequireDocumentDocumentTemplateDto
                                {
                                    Id = d.Id,
                                    Name = d.Name.Value,
                                    Description = d.Description.Value,
                                }).ToList(),
                ListenEvents = x.ListenEvents.Select(x => new ListenEventDto
                {
                    Event = new EnumerationDto
                    {
                        Id = x.EventId,
                        Name = EmployeeEvent.FromValue(x.EventId)!.Name
                    },
                    Status = x.Status
                }).ToArray()
            }).FirstOrDefaultAsync() ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(RequireDocuments), requireDocumentId.ToString()));

            var association = await GetByIdAssociationsByType(companyId, result.Association.Id, result.AssociationType.Id);
            
            result = result with {
                Association = association,
            };

            return result;
        }


        public async Task<IEnumerable<AssociationDto>> GetAllAssociationsByType(Guid companyId, int associationTypeId)
        {
            var associationType = AssociationType.FromValue<AssociationType>(associationTypeId)
                ?? throw new DomainException(this, DomainErrors.FieldInvalid(nameof(AssociationType), associationTypeId.ToString()));

            
            if(associationType == AssociationType.Role)
            {
               var query = _context.Roles.AsNoTracking().Where(x => x.CompanyId == companyId);
                var result = await query.Select(x => new AssociationDto
                {
                    Id = x.Id,
                    Name = x.Name.Value,
                }).ToArrayAsync();
                return result;
            }
            if (associationType == AssociationType.Workplace)
            {
                var query = _context.Workplaces.AsNoTracking().Where(x => x.CompanyId == companyId);
                var result = await query.Select(x => new AssociationDto
                {
                    Id = x.Id,
                    Name = x.Name.Value,
                }).ToArrayAsync();
                return result;
            }
            throw new NotImplementedException($"The association get to {associationType.Name} not be implemented");
        }

        public async Task<AssociationDto> GetByIdAssociationsByType(Guid companyId, Guid associationId, int associationTypeId)
        {
            var associationType = AssociationType.FromValue<AssociationType>(associationTypeId)
                ?? throw new DomainException(this, DomainErrors.FieldInvalid(nameof(AssociationType), associationTypeId.ToString()));


            if (associationType == AssociationType.Role)
            {
                var query = _context.Roles.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == associationId);
                var result = await query.Select(x => new AssociationDto
                {
                    Id = x.Id,
                    Name = x.Name.Value,
                }).SingleOrDefaultAsync()
                 ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Role), associationId.ToString()));
                return result;
            }
            if (associationType == AssociationType.Workplace)
            {
                var query = _context.Workplaces.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == associationId);
                var result = await query.Select(x => new AssociationDto
                {
                    Id = x.Id,
                    Name = x.Name.Value,
                }).SingleOrDefaultAsync() 
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Workplace), associationId.ToString()));
                return result;
            }
            throw new NotImplementedException($"The association get to {associationType.Name} not be implemented");
        }

    }
}
