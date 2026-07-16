using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests.Mothers
{
    public static class DocumentMother
    {
        // Ids fixos e distinguíveis: se dois trocarem de lugar, a falha diz qual virou qual, em vez de comparar
        // dois Guids aleatórios.
        public static readonly Guid Id = new("11111111-1111-1111-1111-111111111111");
        public static readonly Guid EmployeeId = new("22222222-2222-2222-2222-222222222222");
        public static readonly Guid CompanyId = new("33333333-3333-3333-3333-333333333333");
        public static readonly Guid RequiredDocumentId = new("44444444-4444-4444-4444-444444444444");
        public static readonly Guid DocumentTemplateId = new("55555555-5555-5555-5555-555555555555");

        /// <summary>
        /// Cria um Document com os ids fixos, passando-os <b>posicionalmente</b>.
        ///
        /// Simple() usa argumentos nomeados, o que o deixa imune a uma troca de ordem no Create — mas a produção
        /// chama posicionalmente, e são cinco Guid seguidos. Trocar dois compila e passa despercebido. Esta
        /// variante existe para que DocumentIdentityTests fixe essa ordem.
        /// </summary>
        public static Document WithKnownIds()
            => Document.Create(
                Id,
                EmployeeId,
                CompanyId,
                RequiredDocumentId,
                DocumentTemplateId,
                "Documento Teste",
                "Descrição do documento");

        // A configuração de competência não mora mais no Document — ela é passada por operação
        // (NewDocumentUnit/UpdateDocumentUnitDetails), espelhando a leitura ao vivo do template.
        public static Document Simple()
            => Document.Create(
                Guid.NewGuid(),
                employeeId: Guid.NewGuid(),
                companyId: Guid.NewGuid(),
                requiredDocumentId: Guid.NewGuid(),
                documentTemplateId: Guid.NewGuid(),
                name: "Documento Teste",
                description: "Descrição do documento");
    }
}
