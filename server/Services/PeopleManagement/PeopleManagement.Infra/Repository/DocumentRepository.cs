using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class DocumentRepository(PeopleManagementContext context) : Repository<Document>(context), IDocumentRepository
    {
        public async Task<List<DocumentStatus>> GetAllStatusByEmployeeAsync(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default)
        {
            // Carrega as entidades (com tracking) em vez de projetar x.Status: uma projeção escalar ignora o
            // identity map e lê o valor JÁ COMMITADO no banco, devolvendo o status antigo de um documento que
            // foi alterado mas ainda não persistido (os eventos de domínio são despachados ANTES do commit).
            // Carregando as entidades, o EF devolve as instâncias rastreadas com o status em memória (atual),
            // refletindo mudanças em andamento de documentos Modified — não só os Added.
            var dbStatuses = (await context.Documents
                .Where(x => x.EmployeeId == employeeId && x.CompanyId == companyId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken))
                .Select(x => x.Status)
                .ToList();

            var addedStatuses = context.ChangeTracker.Entries<Document>()
                .Where(e => e.State == EntityState.Added
                    && e.Entity.EmployeeId == employeeId
                    && e.Entity.CompanyId == companyId)
                .Select(e => e.Entity.Status)
                .ToList();

            dbStatuses.AddRange(addedStatuses);
            return dbStatuses;
        }
    }
}
