using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies
{
    // Interfaces de capacidade (Interface Segregation): cada consumidor depende apenas da regra que usa.
    // Um DocumentTemplate compõe um conjunto de policies; consumidores as obtêm por capacidade
    // (GetPolicy<IExpirationPolicy>()), não por acesso a campos soltos do template.

    // Marcador comum: permite tipar a API do template (AddPolicy/GetPolicy) sem recorrer a object.
    public interface IDocumentPolicy
    {
    }

    // Vencimento: por quanto tempo a unidade é válida e se pode renovar após vencer.
    public interface IExpirationPolicy : IDocumentPolicy
    {
        TimeSpan Duration { get; }
        bool CanRenew(int renewalCount);
    }

    // Competência: o documento é por período; qual a granularidade e se usa a competência anterior.
    public interface IPeriodPolicy : IDocumentPolicy
    {
        PeriodType PeriodType { get; }
        bool UsePreviousPeriod { get; }
    }

    // Carga horária: duração de trabalho associada ao documento (distribuída em dias úteis).
    public interface IWorkloadPolicy : IDocumentPolicy
    {
        TimeSpan Workload { get; }
    }
}
