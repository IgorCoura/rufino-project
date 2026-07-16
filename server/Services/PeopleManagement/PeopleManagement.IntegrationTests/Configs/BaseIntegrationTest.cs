using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.IntegrationTests.Configs
{
    // Base comum a todas as classes de teste de integração. Recebe a factory compartilhada (collection fixture)
    // e, ao final de CADA teste, reseta o schema people_management via Respawn — garantindo isolamento
    // (sem poluição de estado entre os [Fact], que antes causava violações de unique constraint).
    //
    // Concentra também os helpers reaproveitados pela suíte (seed/HTTP/verificação), evitando o boilerplate
    // que se repetia em cada teste: obtenção de contexto/cliente, montagem do multipart de PDF, releitura em
    // scope novo e verificação de blob no storage.
    public abstract class BaseIntegrationTest : IAsyncLifetime
    {
        // PDF de referência usado pelos testes de upload (copiado para o output em DataForTests/).
        private static readonly string PdfSamplePath = Path.Combine("DataForTests", "199f760b-601d-4a05-aee4-d0a9dbcc6b4d.pdf");

        protected readonly PeopleManagementWebApplicationFactory _factory;

        protected BaseIntegrationTest(PeopleManagementWebApplicationFactory factory)
        {
            _factory = factory;
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync() => await _factory.ResetDatabaseAsync();

        // Contexto para seed/escrita (change tracking preservado durante o Arrange). O scope é descartado
        // no reset do fim do teste pela factory.
        protected PeopleManagementContext GetContext() => _factory.GetContext();

        protected HttpClient CreateClient() => _factory.CreateClient();

        // Monta um multipart/form-data com o PDF de referência em "formFile" mais os campos informados.
        // O chamador é dono do conteúdo (use `using var content = ...`).
        protected static MultipartFormDataContent PdfMultipartContent(params (string Name, string Value)[] fields)
        {
            var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(File.ReadAllBytes(PdfSamplePath));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "formFile", Path.GetFileName(PdfSamplePath));

            foreach (var (name, value) in fields)
                content.Add(new StringContent(value), name);

            return content;
        }

        // Relê um Document em um scope NOVO com AsNoTracking(), refletindo o que foi de fato persistido
        // (não o que está no change tracker da requisição). Traz TODAS as DocumentsUnits, sem ordem garantida:
        // um chamador que asserta sobre uma unidade específica deve selecioná-la por Id (ex.: First(u => u.Id == x)),
        // não confiar em First()/ordem do banco.
        protected async Task<Document> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
            return await context.Documents.AsNoTracking()
                .Include(x => x.DocumentsUnits)
                .FirstAsync(x => x.Id == documentId, cancellationToken);
        }

        // Confirma que o blob foi disponibilizado no storage (download com conteúdo).
        protected async Task AssertBlobExistsAsync(string nameWithExtension, Guid companyId, CancellationToken cancellationToken = default)
        {
            using var scope = _factory.Services.CreateScope();
            var blobService = scope.ServiceProvider.GetRequiredService<IBlobService>();

            var stream = await blobService.DownloadAsync(nameWithExtension, companyId.ToString(), cancellationToken);

            Assert.NotNull(stream);
            Assert.True(stream.Length > 0);
        }
    }
}
