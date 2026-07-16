using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Mothers;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Policies
{
    /// <summary>
    /// A assinatura virou policy: presença = aceita assinatura, e os locais moram dentro dela. AcceptsSignature
    /// e PlaceSignatures passam a ser derivados. O par (aceite + locais) continua entrando por parâmetro, nos
    /// dois caminhos — não pelo bloco de policies.
    /// </summary>
    public class DocumentTemplateSignaturePolicyTests
    {
        [Fact]
        public void Create_AcceptingSignature_ComposesSignaturePolicy()
        {
            var template = DocumentTemplateMother.Simple(acceptsSignature: true);

            Assert.True(template.HasPolicy<ISignaturePolicy>());
            Assert.True(template.AcceptsSignature);
            Assert.True(template.IsSignable);
        }

        [Fact]
        public void Create_NotAcceptingSignature_ComposesNoSignaturePolicy()
        {
            var template = DocumentTemplateMother.Simple(acceptsSignature: false);

            Assert.False(template.HasPolicy<ISignaturePolicy>());
            Assert.False(template.AcceptsSignature);
            Assert.Empty(template.PlaceSignatures);
        }

        [Fact]
        public void Create_WithPlaceSignatures_ExposesThemThroughThePolicy()
        {
            var place = DocumentTemplateMother.Place();

            var template = DocumentTemplateMother.Simple(acceptsSignature: true, placeSignatures: [place]);

            Assert.Equal(place, Assert.Single(template.PlaceSignatures));
            Assert.Equal(place, Assert.Single(template.GetPolicy<ISignaturePolicy>()!.PlaceSignatures));
        }

        // A contradição some do estado persistido, mas o caller ainda informa os dois separados — a checagem
        // sobrevive na fronteira.
        [Fact]
        public void Create_WithPlaceSignaturesButNotAcceptingSignature_Throws()
        {
            Assert.Throws<DomainException>(() =>
                DocumentTemplateMother.Simple(acceptsSignature: false, placeSignatures: [DocumentTemplateMother.Place()]));
        }

        [Fact]
        public void Edit_TurningSignatureOff_DropsThePolicyAndItsPlaces()
        {
            var template = DocumentTemplateMother.Simple(acceptsSignature: true, placeSignatures: [DocumentTemplateMother.Place()]);

            template.Edit("NR01", "Description NR01", 365, 8, DocumentTemplateMother.ValidFileInfo(), false, [], Guid.NewGuid());

            Assert.False(template.HasPolicy<ISignaturePolicy>());
            Assert.Empty(template.PlaceSignatures);
        }

        [Fact]
        public void Edit_TurningSignatureOn_ComposesThePolicy()
        {
            var template = DocumentTemplateMother.Simple(acceptsSignature: false);

            template.Edit("NR01", "Description NR01", 365, 8, DocumentTemplateMother.ValidFileInfo(), true,
                [DocumentTemplateMother.Place()], Guid.NewGuid());

            Assert.True(template.AcceptsSignature);
            Assert.Single(template.PlaceSignatures);
        }

        // O contrato da API informa a assinatura por parâmetro e as outras regras no bloco de policies. Se o
        // bloco mandasse sozinho, todo Edit vindo do app apagaria a assinatura.
        [Fact]
        public void Edit_WithExplicitPolicies_KeepsSignatureComingFromTheParameters()
        {
            var template = DocumentTemplateMother.Simple(acceptsSignature: false);

            template.Edit("NR01", "Description NR01", null, null, DocumentTemplateMother.ValidFileInfo(), true,
                [DocumentTemplateMother.Place()], Guid.NewGuid(),
                usePreviousPeriod: false, policies: [new ExpirationPolicy(TimeSpan.FromDays(30))]);

            Assert.True(template.AcceptsSignature);
            Assert.Single(template.PlaceSignatures);
            Assert.Equal(TimeSpan.FromDays(30), template.GetPolicy<IExpirationPolicy>()!.Duration);
        }

        [Fact]
        public void Edit_WithEmptyPolicySet_StillKeepsSignatureFromTheParameters()
        {
            var template = DocumentTemplateMother.Simple(acceptsSignature: true);

            template.Edit("NR01", "Description NR01", null, null, DocumentTemplateMother.ValidFileInfo(), true, [], Guid.NewGuid(),
                usePreviousPeriod: false, policies: []);

            Assert.True(template.AcceptsSignature);
        }

        // Aceitar assinatura sem posicionamento fixo é legítimo — a policy existe vazia.
        [Fact]
        public void Create_AcceptingSignatureWithoutPlaces_KeepsThePolicyPresent()
        {
            var template = DocumentTemplateMother.Simple(acceptsSignature: true, placeSignatures: []);

            Assert.True(template.HasPolicy<ISignaturePolicy>());
            Assert.Empty(template.PlaceSignatures);
        }
    }
}
