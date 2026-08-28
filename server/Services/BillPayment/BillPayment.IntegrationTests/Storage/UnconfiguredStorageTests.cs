namespace BillPayment.IntegrationTests.Storage;

using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O que a fábrica compartilhada usa quando ninguém configurou o balde.
/// </summary>
/// <remarks>
/// Irmão de <c>UnconfiguredLookupTests</c>, e pelo mesmo motivo: a config de desenvolvimento
/// (<c>appsettings.Development.json</c> + user-secrets da máquina) é lida por esta suíte, e sem
/// estes testes ela passaria a apontar a fábrica compartilhada para um balde de verdade — em
/// silêncio, porque nenhum outro teste grava anexo por aqui.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class UnconfiguredStorageTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a1"));

    public UnconfiguredStorageTests(IntegrationTestWebAppFactory factory) : base(factory, Tenant.Value) { }

    // Sem balde configurado, guardar artefato falha alto: guardar em lugar nenhum sem avisar
    // faria o sistema pagar boleto cujo original ninguém recupera depois.
    [Fact]
    public async Task StoreAsync_WhenStorageIsNotConfigured_ShouldThrow()
    {
        using var scope = Factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IAttachmentStorage>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => storage.StoreAsync(Tenant, "boleto.pdf", "application/pdf", new byte[] { 1, 2, 3 }, default));
    }

    // Ler também falha, e não devolve vazio: um artefato que "não está lá" e um armazenamento
    // ausente pedem reações opostas, e colapsá-los esconderia a falta de configuração.
    [Fact]
    public async Task RetrieveAsync_WhenStorageIsNotConfigured_ShouldThrow()
    {
        using var scope = Factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IAttachmentStorage>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => storage.RetrieveAsync(Tenant, $"tenants/{Tenant.Value:N}/captures/2026/08/x-boleto.pdf", default));
    }

    // Abrir para servir ao usuário também estoura, e NÃO devolve null: null significa "este
    // documento não existe", e diria ao usuário que o comprovante do boleto se perdeu. Aqui o que
    // falta é configuração — a diferença entre um 404 que encerra o assunto e um erro que leva
    // alguém a configurar o balde.
    [Fact]
    public async Task OpenAsync_WhenStorageIsNotConfigured_ShouldThrow()
    {
        using var scope = Factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IAttachmentStorage>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => storage.OpenAsync(Tenant, $"tenants/{Tenant.Value:N}/captures/2026/08/x-boleto.pdf", default));
    }

    // Apagar é a única operação tolerante: o objetivo da purga — o arquivo não existir — já está
    // satisfeito, e falhar aqui travaria a limpeza de item que nunca teve arquivo.
    [Fact]
    public async Task RemoveAsync_WhenStorageIsNotConfigured_ShouldSucceed()
    {
        using var scope = Factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IAttachmentStorage>();

        await storage.RemoveAsync(Tenant, "qualquer-chave", default);
    }
}
