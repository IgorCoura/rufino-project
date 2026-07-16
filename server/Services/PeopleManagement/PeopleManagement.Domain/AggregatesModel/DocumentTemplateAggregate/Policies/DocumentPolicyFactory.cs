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
            // A limitada casa antes da interface: casar por IExpirationPolicy engoliria a variante e perderia o
            // MaxRenewals no jsonb. MaxRenewals null nos params = renovação indefinida (ExpirationPolicy).
            ExpirationLimitedPolicy l => new DocumentPolicy(PolicyType.Expiration, Serialize(new ExpirationParams(l.Duration.Ticks, l.MaxRenewals))),
            IExpirationPolicy e => new DocumentPolicy(PolicyType.Expiration, Serialize(new ExpirationParams(e.Duration.Ticks, null))),
            IPeriodPolicy p => new DocumentPolicy(PolicyType.Period, Serialize(new PeriodParams(p.PeriodType.Id, p.UsePreviousPeriod))),
            IWorkloadPolicy w => new DocumentPolicy(PolicyType.Workload, Serialize(new WorkloadParams(w.Workload.Ticks))),
            ISignaturePolicy s => new DocumentPolicy(PolicyType.Signature, Serialize(ToSignatureParams(s))),
            _ => throw new DomainException(nameof(DocumentPolicyFactory), DomainErrors.FieldInvalid(nameof(policy), policy.GetType().Name))
        };

        public static IDocumentPolicy ToPolicy(DocumentPolicy record) => record.Type.Id switch
        {
            var id when id == PolicyType.Expiration.Id => ToExpirationPolicy(Deserialize<ExpirationParams>(record.Params)),
            var id when id == PolicyType.Period.Id => ToPeriodPolicy(Deserialize<PeriodParams>(record.Params)),
            var id when id == PolicyType.Workload.Id => new WorkloadPolicy(TimeSpan.FromTicks(Deserialize<WorkloadParams>(record.Params).WorkloadTicks)),
            var id when id == PolicyType.Signature.Id => ToSignaturePolicy(Deserialize<SignatureParams>(record.Params)),
            _ => throw new DomainException(nameof(DocumentPolicyFactory), DomainErrors.FieldInvalid(nameof(PolicyType), record.Type.ToString()))
        };

        // MaxRenewals ausente/null (o caso das linhas gravadas antes da Fase 3b e do backfill) = renovação
        // indefinida; presente = renovação limitada. É o discriminador entre as duas policies de vencimento.
        private static IExpirationPolicy ToExpirationPolicy(ExpirationParams p)
            => p.MaxRenewals is null
                ? new ExpirationPolicy(TimeSpan.FromTicks(p.DurationTicks))
                : new ExpirationLimitedPolicy(TimeSpan.FromTicks(p.DurationTicks), p.MaxRenewals.Value);

        private static PeriodPolicy ToPeriodPolicy(PeriodParams p)
            => new(PeriodType.CreateFromValue(p.PeriodTypeId), p.UsePreviousPeriod);

        private static SignatureParams ToSignatureParams(ISignaturePolicy policy)
            => new(policy.PlaceSignatures
                .Select(x => new PlaceSignatureParams(
                    x.Type.Id, x.Page.Value, x.RelativePositionBotton.Value,
                    x.RelativePositionLeft.Value, x.RelativeSizeX.Value, x.RelativeSizeY.Value))
                .ToList());

        private static SignaturePolicy ToSignaturePolicy(SignatureParams p)
            => new(p.PlaceSignatures.Select(x => PlaceSignature.Create(
                TypeSignature.FromValue<TypeSignature>(x.TypeId), x.Page, x.RelativePositionBotton,
                x.RelativePositionLeft, x.RelativeSizeX, x.RelativeSizeY)));

        private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

        private static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options)!;

        // Durações viajam em ticks (não em TimeSpan) para que o backfill da migration consiga reproduzir
        // exatamente o mesmo payload a partir das colunas interval, sem depender do formato textual do STJ.
        // MaxRenewals é nullable e opcional: linhas antigas (sem a chave) desserializam como null = indefinido,
        // então a Fase 3b não precisa de migração de dados.
        private sealed record ExpirationParams(long DurationTicks, int? MaxRenewals = null);
        private sealed record PeriodParams(int PeriodTypeId, bool UsePreviousPeriod);
        private sealed record WorkloadParams(long WorkloadTicks);

        // Os PlaceSignature viajam achatados: os VOs (TypeSignature, Number) não têm construtor público que o
        // STJ alcance, e prender o formato do jsonb à forma interna deles deixaria o payload refém de refatoração.
        private sealed record SignatureParams(List<PlaceSignatureParams> PlaceSignatures);
        private sealed record PlaceSignatureParams(
            int TypeId, double Page, double RelativePositionBotton,
            double RelativePositionLeft, double RelativeSizeX, double RelativeSizeY);
    }
}
