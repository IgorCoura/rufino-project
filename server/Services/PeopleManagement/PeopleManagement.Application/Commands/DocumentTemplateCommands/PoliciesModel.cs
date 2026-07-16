using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
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
    /// </summary>
    public record PoliciesModel(
        ExpirationPolicyModel? Expiration = null,
        WorkloadPolicyModel? Workload = null,
        PeriodPolicyModel? Period = null)
    {
        public IEnumerable<IDocumentPolicy> ToPolicies()
        {
            if (Expiration is not null)
                yield return Expiration.ToPolicy();

            if (Workload is not null)
                yield return Workload.ToPolicy();

            if (Period is not null)
                yield return Period.ToPolicy();
        }
    }

    // MaxRenewals opcional: ausente/null = renovação indefinida (ExpirationPolicy); informado = renovação
    // limitada (ExpirationLimitedPolicy). Espelha o discriminador do jsonb.
    public record ExpirationPolicyModel(double DurationInDays, int? MaxRenewals = null)
    {
        public IDocumentPolicy ToPolicy() => MaxRenewals is null
            ? new ExpirationPolicy(TimeSpan.FromDays(DurationInDays))
            : new ExpirationLimitedPolicy(TimeSpan.FromDays(DurationInDays), MaxRenewals.Value);
    }

    public record WorkloadPolicyModel(double Hours)
    {
        public IDocumentPolicy ToPolicy() => new WorkloadPolicy(TimeSpan.FromHours(Hours));
    }

    public record PeriodPolicyModel(int PeriodTypeId, bool UsePreviousPeriod)
    {
        public IDocumentPolicy ToPolicy() => new PeriodPolicy(PeriodType.CreateFromValue(PeriodTypeId), UsePreviousPeriod);
    }
}
