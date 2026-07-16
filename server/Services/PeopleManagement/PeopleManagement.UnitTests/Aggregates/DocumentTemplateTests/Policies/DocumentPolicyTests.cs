using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Policies
{
    /// <summary>
    /// Modelo de policies (Fase 2): comportamento das policies e round-trip pela forma persistida
    /// (DocumentPolicyFactory) — serialização em jsonb (Opção 1) preserva os parâmetros.
    /// </summary>
    public class DocumentPolicyTests
    {
        [Fact]
        public void ExpirationPolicy_CanRenew_IsAlwaysTrue()
        {
            var policy = new ExpirationPolicy(TimeSpan.FromDays(365));

            Assert.True(policy.CanRenew(0));
            Assert.True(policy.CanRenew(100));
        }

        [Fact]
        public void ToPersistence_ExpirationPolicy_UsesExpirationDiscriminator()
        {
            var record = DocumentPolicyFactory.ToPersistence(new ExpirationPolicy(TimeSpan.FromDays(30)));

            Assert.Equal(PolicyType.Expiration, record.Type);
        }

        [Fact]
        public void RoundTrip_ExpirationPolicy_PreservesDuration()
        {
            var record = DocumentPolicyFactory.ToPersistence(new ExpirationPolicy(TimeSpan.FromDays(365)));

            var restored = Assert.IsType<ExpirationPolicy>(DocumentPolicyFactory.ToPolicy(record));
            Assert.Equal(TimeSpan.FromDays(365), restored.Duration);
        }

        [Fact]
        public void RoundTrip_PeriodPolicy_PreservesPeriodTypeAndUsePreviousPeriod()
        {
            var record = DocumentPolicyFactory.ToPersistence(new PeriodPolicy(PeriodType.Monthly, usePreviousPeriod: true));

            var restored = Assert.IsType<PeriodPolicy>(DocumentPolicyFactory.ToPolicy(record));
            Assert.Equal(PeriodType.Monthly, restored.PeriodType);
            Assert.True(restored.UsePreviousPeriod);
        }

        [Fact]
        public void RoundTrip_WorkloadPolicy_PreservesWorkload()
        {
            var record = DocumentPolicyFactory.ToPersistence(new WorkloadPolicy(TimeSpan.FromHours(8)));

            var restored = Assert.IsType<WorkloadPolicy>(DocumentPolicyFactory.ToPolicy(record));
            Assert.Equal(TimeSpan.FromHours(8), restored.Workload);
        }

        [Fact]
        public void RoundTrip_SignaturePolicy_PreservesEveryPlaceSignatureField()
        {
            var place = PlaceSignature.Create(TypeSignature.Visa, 3, 10.5, 20.25, 30, 40);
            var record = DocumentPolicyFactory.ToPersistence(new SignaturePolicy([place]));

            var restored = Assert.IsType<SignaturePolicy>(DocumentPolicyFactory.ToPolicy(record));

            var persisted = Assert.Single(restored.PlaceSignatures);
            Assert.Equal(TypeSignature.Visa, persisted.Type);
            Assert.Equal(3, persisted.Page.Value);
            Assert.Equal(10.5, persisted.RelativePositionBotton.Value);
            Assert.Equal(20.25, persisted.RelativePositionLeft.Value);
            Assert.Equal(30, persisted.RelativeSizeX.Value);
            Assert.Equal(40, persisted.RelativeSizeY.Value);
        }

        [Fact]
        public void RoundTrip_SignaturePolicy_PreservesEveryPlaceSignature()
        {
            var policy = new SignaturePolicy([
                PlaceSignature.Create(TypeSignature.Signature, 1, 1, 1, 1, 1),
                PlaceSignature.Create(TypeSignature.Visa, 2, 2, 2, 2, 2),
            ]);
            var record = DocumentPolicyFactory.ToPersistence(policy);

            var restored = Assert.IsType<SignaturePolicy>(DocumentPolicyFactory.ToPolicy(record));

            Assert.Equal(2, restored.PlaceSignatures.Count);
        }

        // Aceitar assinatura sem definir local é legítimo: assina sem posicionamento fixo.
        [Fact]
        public void RoundTrip_SignaturePolicyWithoutPlaces_StaysPresentAndEmpty()
        {
            var record = DocumentPolicyFactory.ToPersistence(new SignaturePolicy());

            var restored = Assert.IsType<SignaturePolicy>(DocumentPolicyFactory.ToPolicy(record));

            Assert.Empty(restored.PlaceSignatures);
        }

        [Fact]
        public void ToPersistence_SignaturePolicy_UsesSignatureDiscriminator()
        {
            var record = DocumentPolicyFactory.ToPersistence(new SignaturePolicy());

            Assert.Equal(PolicyType.Signature, record.Type);
        }

        // Não há teste do fallback de ToPolicy: depois que Generation saiu, todo PolicyType tem suporte na
        // factory, e CreateFromValue recusa qualquer id fora da lista — o estado que aquele arm cobre deixou de
        // ser construtível. Ele fica como guarda para quem adicionar um PolicyType e esquecer a factory.

        [Fact]
        public void ToPersistence_WithUnsupportedPolicy_Throws()
        {
            Assert.Throws<DomainException>(() => DocumentPolicyFactory.ToPersistence(new UnsupportedPolicy()));
        }

        // Policy que não implementa nenhuma capacidade conhecida — exercita o arm de fallback da factory.
        private sealed class UnsupportedPolicy : IDocumentPolicy
        {
        }
    }
}
