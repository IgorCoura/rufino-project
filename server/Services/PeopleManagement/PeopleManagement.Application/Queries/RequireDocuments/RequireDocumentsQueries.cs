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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static PeopleManagement.Application.Queries.Department.DepartmentDtos;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using RequireDoc = PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.RequireDocuments;

namespace PeopleManagement.Application.Queries.RequireDocuments
{
    public class RequireDocumentsQueries(IDbContextFactory<PeopleManagementContext> factory) : IRequireDocumentsQueries
    {
        private IDbContextFactory<PeopleManagementContext> _factory = factory;

        public async Task<IEnumerable<RequiredWithDocumentListDto>> GetAllWithDocumentList(Guid companyId, Guid employeeId)
        {
            using var context = _factory.CreateDbContext();
            var employeeQuery = 
                from em in context.Employees.AsNoTracking()
                where em.Id == employeeId 
                select em.GetAllPossibleAssociationIds();

            var allPossibleAssociationIds = await employeeQuery.FirstOrDefaultAsync() ?? [];

            var query = from req in context.RequireDocuments.AsNoTracking()
                        where req.CompanyId == companyId && allPossibleAssociationIds.Contains(req.AssociationId)
                        select new RequiredWithDocumentListDto
                        {
                            Id = req.Id,
                            Name = req.Name.Value,
                            Description = req.Description.Value,
                            CompanyId = req.CompanyId,
                            Documents = (from d in context.Documents.AsNoTracking()
                                         where d.RequiredDocumentId == req.Id && d.EmployeeId == employeeId
                                         select new RequireDocumentSimpleDocumentDto
                                         {
                                             Id = d.Id,
                                             Name = d.Name.Value,
                                             Description = d.Description.Value,
                                             Status = d.Status,
                                             EmployeeId = d.EmployeeId,
                                             CreateAt = d.CreatedAt,
                                             UpdateAt = d.UpdatedAt
                                         }).ToList(),
                           
                        };

            var reqDocumnets = await query.ToListAsync();

            var tasks = reqDocumnets.Select(async x =>
            {
                var status = await GetDocumentRepresentingStatusAsync(x.Id, employeeId, companyId);
                return x with
                {
                    DocumentsStatus = status
                };
            });

            return await Task.WhenAll(tasks);

        }
        public async Task<IEnumerable<RequireDocumentSimpleDto>> GetAllSimple(Guid companyId)
        {
            using var context = _factory.CreateDbContext();
            var query = context.RequireDocuments.AsNoTracking().Where(x => x.CompanyId == companyId);

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
            using var context = _factory.CreateDbContext();
            var query = context.RequireDocuments.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == requireDocumentId);
                      

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
                DocumentsTemplates = context.DocumentTemplates
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
                        Name = RequireDoc.GetEventName(x.EventId)
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
            using var context = _factory.CreateDbContext();
            var associationType = AssociationType.FromValue<AssociationType>(associationTypeId)
                ?? throw new DomainException(this, DomainErrors.FieldInvalid(nameof(AssociationType), associationTypeId.ToString()));

            
            if(associationType == AssociationType.Role)
            {
               var query = context.Roles.AsNoTracking().Where(x => x.CompanyId == companyId);
                var result = await query.Select(x => new AssociationDto
                {
                    Id = x.Id,
                    Name = x.Name.Value,
                }).ToArrayAsync();
                return result;
            }
            if (associationType == AssociationType.Workplace)
            {
                var query = context.Workplaces.AsNoTracking().Where(x => x.CompanyId == companyId);
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
            using var context = _factory.CreateDbContext();
            var associationType = AssociationType.FromValue<AssociationType>(associationTypeId)
                ?? throw new DomainException(this, DomainErrors.FieldInvalid(nameof(AssociationType), associationTypeId.ToString()));


            if (associationType == AssociationType.Role)
            {
                var query = context.Roles.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == associationId);
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
                var query = context.Workplaces.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == associationId);
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

        private async Task<EnumerationDto> GetDocumentRepresentingStatusAsync(Guid requiredDocumentId, Guid employeeId, Guid companyId, CancellationToken cancellationToken = default)
        {
            using var context = _factory.CreateDbContext();
            var documentsStatus = await context.Documents.Where(x => x.RequiredDocumentId == requiredDocumentId && x.EmployeeId == employeeId && x.CompanyId == companyId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.Status)
                .ToListAsync(cancellationToken);

            var status = Domain.AggregatesModel.DocumentAggregate.Document.GetRepresentingStatus(documentsStatus);
            return new EnumerationDto
            {
                Id = status.Id,
                Name = status.Name
            };

        }

    }
}
