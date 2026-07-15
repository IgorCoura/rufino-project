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

        public static DocumentPolicy ToPersistence(IDocumentPolicy policy) => policy switch
        {
            IExpirationPolicy e => new DocumentPolicy(PolicyType.Expiration, Serialize(new ExpirationParams(e.Duration.Ticks))),
            IPeriodPolicy p => new DocumentPolicy(PolicyType.Period, Serialize(new PeriodParams(p.PeriodType.Id, p.UsePreviousPeriod))),
            IWorkloadPolicy w => new DocumentPolicy(PolicyType.Workload, Serialize(new WorkloadParams(w.Workload.Ticks))),
            _ => throw new DomainException(nameof(DocumentPolicyFactory), DomainErrors.FieldInvalid(nameof(policy), policy.GetType().Name))
        };

        public static IDocumentPolicy ToPolicy(DocumentPolicy record) => record.Type.Id switch
        {
            var id when id == PolicyType.Expiration.Id => new ExpirationPolicy(TimeSpan.FromTicks(Deserialize<ExpirationParams>(record.Params).DurationTicks)),
            var id when id == PolicyType.Period.Id => ToPeriodPolicy(Deserialize<PeriodParams>(record.Params)),
            var id when id == PolicyType.Workload.Id => new WorkloadPolicy(TimeSpan.FromTicks(Deserialize<WorkloadParams>(record.Params).WorkloadTicks)),
            _ => throw new DomainException(nameof(DocumentPolicyFactory), DomainErrors.FieldInvalid(nameof(PolicyType), record.Type.ToString()))
        };

        private static PeriodPolicy ToPeriodPolicy(PeriodParams p)
            => new(PeriodType.CreateFromValue(p.PeriodTypeId), p.UsePreviousPeriod);

        private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

        private static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options)!;

        // Durações viajam em ticks (não em TimeSpan) para que o backfill da migration consiga reproduzir
        // exatamente o mesmo payload a partir das colunas interval, sem depender do formato textual do STJ.
        private sealed record ExpirationParams(long DurationTicks);
        private sealed record PeriodParams(int PeriodTypeId, bool UsePreviousPeriod);
        private sealed record WorkloadParams(long WorkloadTicks);
    }
}
