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

    // Vencimento: por quanto tempo a unidade é válida e se o documento ainda tem ciclo de validade a gastar.
    //
    // O teto NÃO é permissão para renovar — renovar, substituir, depreciar e invalidar continuam sempre
    // disponíveis ao RH. Esgotados os ciclos, as unidades novas simplesmente nascem SEM validade, como num
    // template sem regra de vencimento, e o documento nunca mais vence.
    public interface IExpirationPolicy : IDocumentPolicy
    {
        TimeSpan Duration { get; }
        bool HasValidityCycleLeft(int renewalCount);
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

    // Assinatura: o documento pode ser assinado, e onde as assinaturas são posicionadas.
    // A policy carrega os locais, então local de assinatura sem aceitar assinatura é inexprimível.
    public interface ISignaturePolicy : IDocumentPolicy
    {
        IReadOnlyList<PlaceSignature> PlaceSignatures { get; }
    }

    // Depreciação em novo contrato: quando um novo contrato de trabalho começa, os documentos entregues
    // (unidades OK) do contrato anterior deixam de valer. Regra sem parâmetro — só presença/ausência.
    public interface INewContractDeprecationPolicy : IDocumentPolicy
    {
    }
}
