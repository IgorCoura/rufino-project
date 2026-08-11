namespace BillPayment.IntegrationTests.DocumentIntelligence;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O que a fábrica compartilhada usa quando o extrator de visão não está configurado.
/// </summary>
/// <remarks>
/// <para>
/// Irmão de <c>UnconfiguredLookupTests</c> e <c>UnconfiguredStorageTests</c>, e o mais
/// necessário dos três: a chave do Gemini vive no <c>user-secrets</c> da máquina e o
/// <c>appsettings.Development.json</c> liga o provedor — que a suíte lê. Sem a blindagem da
/// fábrica, cada execução dos testes gastaria cota de uma conta gratuita, e os testes deixariam
/// de ser determinísticos.
/// </para>
/// <para>
/// Estes testes são a prova de que a blindagem vence a configuração de desenvolvimento.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class UnconfiguredDocumentIntelligenceTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-0000000000d1"));

    public UnconfiguredDocumentIntelligenceTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // Sem provedor configurado, o extrator se declara desligado — e quem chama nem monta o
    // payload, em vez de serializar megabytes em base64 para descartar em seguida.
    [Fact]
    public void IsEnabled_WhenNoProviderIsConfigured_ShouldBeFalse()
    {
        using var scope = Factory.Services.CreateScope();
        var vision = scope.ServiceProvider.GetRequiredService<IDocumentIntelligence>();

        Assert.False(vision.IsEnabled);
    }

    // E extrair devolve vazio em vez de falhar: não ter IA apenas faz a cascata terminar onde ela
    // já terminava antes da 2.4. É degradação, não erro — ao contrário do armazenamento, cuja
    // ausência perderia um comprovante que ninguém recupera.
    [Fact]
    public async Task ExtractAsync_WhenNoProviderIsConfigured_ShouldReturnNoCandidates()
    {
        using var scope = Factory.Services.CreateScope();
        var vision = scope.ServiceProvider.GetRequiredService<IDocumentIntelligence>();

        var payload = DocumentPayload.From(Tenant, new byte[] { 1, 2, 3 }, DocumentPayload.PDF);
        var result = await vision.ExtractAsync(payload, ExtractionHints.None, default);

        Assert.False(result.HasCandidates);
    }
}
