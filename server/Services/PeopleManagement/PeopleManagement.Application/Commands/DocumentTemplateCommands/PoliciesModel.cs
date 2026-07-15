using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;

namespace PeopleManagement.Application.Commands.DocumentTemplateCommands
{
    /// <summary>
    /// Conjunto de regras (policies) de um DocumentTemplate no contrato da API. Cada bloco é opcional e a
    /// presença dele significa que a regra está ativa — espelha o Composite do domínio.
    ///
    /// Omitir "policies" no payload = caminho legado: as regras são derivadas de DocumentValidityDurationInDays
    /// e WorkloadInHours. Enviar "policies" (mesmo vazio) = as policies mandam, e aqueles campos passam a ser
    /// apenas reflexo delas.
    ///
    /// PeriodPolicy não está aqui de propósito: o domínio ainda não a consome (quem decide competência é o
    /// fluxo do evento), então aceitá-la seria expor configuração sem efeito.
    /// </summary>
    public record PoliciesModel(ExpirationPolicyModel? Expiration = null, WorkloadPolicyModel? Workload = null)
    {
        public IEnumerable<IDocumentPolicy> ToPolicies()
        {
            if (Expiration is not null)
                yield return Expiration.ToPolicy();

            if (Workload is not null)
                yield return Workload.ToPolicy();
        }
    }

    public record ExpirationPolicyModel(double DurationInDays)
    {
        public IDocumentPolicy ToPolicy() => new ExpirationPolicy(TimeSpan.FromDays(DurationInDays));
    }

    public record WorkloadPolicyModel(double Hours)
    {
        public IDocumentPolicy ToPolicy() => new WorkloadPolicy(TimeSpan.FromHours(Hours));
    }
}
