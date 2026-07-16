using PeopleManagement.UnitTests.Aggregates.DocumentTests.Mothers;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    /// <summary>
    /// Fixa qual Guid vai em qual propriedade do Document.
    ///
    /// O Create recebe cinco Guid seguidos e posicionais. Trocar dois de lugar compila, não gera aviso, e passa
    /// por toda a suíte — porque o resto dos testes usa ids aleatórios, e dois valores aleatórios trocados entre
    /// si continuam satisfazendo qualquer assertion. O efeito só aparece em produção: documento apontando para o
    /// template errado.
    ///
    /// Estes testes existem para que mudanças na assinatura do Create — como as da Fase 3 — não consigam
    /// reordenar os ids em silêncio.
    /// </summary>
    public class DocumentIdentityTests
    {
        [Fact]
        public void Create_AssignsTheIdentityToTheDocument()
        {
            var document = DocumentMother.WithKnownIds();

            Assert.Equal(DocumentMother.Id, document.Id);
        }

        [Fact]
        public void Create_AssignsTheEmployeeItBelongsTo()
        {
            var document = DocumentMother.WithKnownIds();

            Assert.Equal(DocumentMother.EmployeeId, document.EmployeeId);
        }

        [Fact]
        public void Create_AssignsTheOwningCompany()
        {
            var document = DocumentMother.WithKnownIds();

            Assert.Equal(DocumentMother.CompanyId, document.CompanyId);
        }

        // Estes dois são vizinhos na assinatura e ambos Guid — é exatamente aqui que a troca passa despercebida.
        [Fact]
        public void Create_AssignsTheRequireDocumentsItCameFrom()
        {
            var document = DocumentMother.WithKnownIds();

            Assert.Equal(DocumentMother.RequiredDocumentId, document.RequiredDocumentId);
        }

        [Fact]
        public void Create_AssignsTheTemplateItWasGeneratedFrom()
        {
            var document = DocumentMother.WithKnownIds();

            Assert.Equal(DocumentMother.DocumentTemplateId, document.DocumentTemplateId);
        }

        [Fact]
        public void Create_KeepsEveryIdentityDistinct()
        {
            var document = DocumentMother.WithKnownIds();

            var identities = new[]
            {
                document.Id, document.EmployeeId, document.CompanyId,
                document.RequiredDocumentId, document.DocumentTemplateId,
            };

            Assert.Equal(identities.Length, identities.Distinct().Count());
        }

        // Name e Description normalizam o texto para maiúsculas — comportamento dos VOs, fixado aqui porque foi
        // descoberto ao escrever este teste e não estava coberto em lugar nenhum.
        [Fact]
        public void Create_AssignsNameAndDescription()
        {
            var document = DocumentMother.WithKnownIds();

            Assert.Equal("DOCUMENTO TESTE", document.Name.Value);
            Assert.Equal("DESCRIÇÃO DO DOCUMENTO", document.Description.Value);
        }
    }
}
