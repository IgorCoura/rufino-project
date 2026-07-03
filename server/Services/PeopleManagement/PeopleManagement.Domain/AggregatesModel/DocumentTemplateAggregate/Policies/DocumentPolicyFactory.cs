using System.Text.Json;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies
{
    /// <summary>
    /// Converte entre as policies tipadas (comportamento) e a forma persistida <see cref="DocumentPolicy"/>
    /// (discriminador + parâmetros jsonb). Centraliza a serialização (Opção 1) para que o aggregate exponha
    /// policies por capacidade sem espalhar conhecimento de formato.
    /// </summary>
    public static class DocumentPolicyFactory
    {
        private static readonly JsonSerializerOptions Options = new();

        public static DocumentPolicy ToPersistence(object policy) => policy switch
        {
            ExpirationPolicy e => new DocumentPolicy(PolicyType.Expiration, Serialize(new ExpirationParams(e.Duration))),
            PeriodPolicy p => new DocumentPolicy(PolicyType.Period, Serialize(new PeriodParams(p.PeriodType.Id, p.UsePreviousPeriod))),
            WorkloadPolicy w => new DocumentPolicy(PolicyType.Workload, Serialize(new WorkloadParams(w.Workload))),
            _ => throw new DomainException(nameof(DocumentPolicyFactory), DomainErrors.FieldInvalid(nameof(policy), policy.GetType().Name))
        };

        public static object ToPolicy(DocumentPolicy record) => record.Type.Id switch
        {
            var id when id == PolicyType.Expiration.Id => new ExpirationPolicy(Deserialize<ExpirationParams>(record.Params).Duration),
            var id when id == PolicyType.Period.Id => ToPeriodPolicy(Deserialize<PeriodParams>(record.Params)),
            var id when id == PolicyType.Workload.Id => new WorkloadPolicy(Deserialize<WorkloadParams>(record.Params).Workload),
            _ => throw new DomainException(nameof(DocumentPolicyFactory), DomainErrors.FieldInvalid(nameof(PolicyType), record.Type.ToString()))
        };

        private static PeriodPolicy ToPeriodPolicy(PeriodParams p)
            => new(PeriodType.CreateFromValue(p.PeriodTypeId), p.UsePreviousPeriod);

        private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

        private static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options)!;

        private sealed record ExpirationParams(TimeSpan Duration);
        private sealed record PeriodParams(int PeriodTypeId, bool UsePreviousPeriod);
        private sealed record WorkloadParams(TimeSpan Workload);
    }
}
