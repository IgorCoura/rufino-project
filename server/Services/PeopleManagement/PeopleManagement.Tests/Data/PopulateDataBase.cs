using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Tests.Data
{
    public static class PopulateDataBase
    {
        public static async Task<Guid> InsertCompany(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            var company = Company.Create(
            id,
            "Lucas e Ryan Informática Ltda",
            "Lucas e Ryan Informática",
            "82161379000186",
            Contact.Create(
                "auditoria@lucaseryaninformaticaltda.com.br",
                "1637222844"),
            Address.Create(
                "14093636",
                "Rua José Otávio de Oliveira",
                "776",
                "",
                "Parque dos Flamboyans",
                "Ribeirão Preto",
                "SP",
                "BRASIL")
            );

            await context.Companies.AddAsync(company, cancellationToken);

            return id;
        }

        public static async Task<Guid> InsertRole(this PeopleManagementContext context, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();

            return id;
        }
    }
}
