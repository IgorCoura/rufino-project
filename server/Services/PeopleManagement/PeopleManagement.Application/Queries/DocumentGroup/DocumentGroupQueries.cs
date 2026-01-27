using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Util;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.Base.BaseDtos;
using static PeopleManagement.Application.Queries.DocumentGroup.DocumentGroupDtos;
using static PeopleManagement.Application.Queries.RequireDocuments.RequireDocumentsDtos;
using DocumentEntity = PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Document;


namespace PeopleManagement.Application.Queries.DocumentGroup
{
    public class DocumentGroupQueries(PeopleManagementContext peopleManagementContext) : IDocumentGroupQueries
    {
        private readonly PeopleManagementContext _context = peopleManagementContext;

        public async Task<IEnumerable<DocumentGroupDto>> GetAll(Guid company)
        {
            var query = from d in _context.DocumentGroups.AsNoTracking()
                        where d.CompanyId == company
                        select new DocumentGroupDto
                        {
                            Id = d.Id,
                            Name = d.Name.Value,
                            Description = d.Description.Value,
                            CompanyId = d.CompanyId
                        };


            List<DocumentGroupDto> result = await query.ToListAsync();
            return result;
        }


        public async Task<IEnumerable<DocumentGroupWithDocumentsDto>> GetAllWithDocuments(Guid companyId, Guid employeeId)
        {
            var query = from dg in _context.DocumentGroups.AsNoTracking()
                        join dt in _context.DocumentTemplates.AsNoTracking() on dg.Id equals dt.DocumentGroupId
                        join d in _context.Documents.AsNoTracking() on dt.Id equals d.DocumentTemplateId
                        where d.CompanyId == companyId && d.EmployeeId == employeeId
                        group d by dg into grouped
                        where grouped.Any()
                        select new DocumentGroupWithDocumentsDto
                        {
                            Id = grouped.Key.Id,
                            Name = grouped.Key.Name.Value,
                            Description = grouped.Key.Description.Value,
                            CompanyId = grouped.Key.CompanyId,
                            DocumentsStatus = Base.BaseDtos.EnumerationDto.Empty,
                            Documents = grouped.Select(d => new DocumentDocumentGroupDto
                            {
                                Id = d.Id,
                                Name = d.Name.Value,
                                Description = d.Description.Value,
                                Status = new Base.BaseDtos.EnumerationDto
                                {
                                    Id = d.Status.Id,
                                    Name = d.Status.Name
                                },
                                EmployeeId = d.EmployeeId,
                                CreateAt = d.CreatedAt,
                                UpdateAt = d.UpdatedAt
                            }).OrderByDescending(x => x.CreateAt).ToList()
                        };

            var result = await query.ToListAsync();



            result = result.Select(record =>
            {
                return record with
                {
                    DocumentsStatus = DocumentStatusUtil.GetDocumentGroupStatus(record.Documents.Select(x =>(DocumentStatus)x.Status.Id).ToList())
                };
            }).ToList();    

            return result;
        }

       

    }
}
