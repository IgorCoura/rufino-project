namespace BillPayment.IntegrationTests.Storage;

using Amazon.Runtime;
using Amazon.S3;
using BillPayment.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O que o cliente S3 carrega antes de o primeiro anexo subir.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Monta o contêiner de DI de propósito</strong>, pelo mesmo motivo de
/// <c>AsaasClientConfigurationTests</c>: o resto da suíte troca <c>IAttachmentStorage</c> por um
/// dublê em memória, então nada enxerga o que <c>AddAttachmentStorage</c> configura no
/// <c>AmazonS3Config</c> — e é exatamente ali que moram os defeitos que só aparecem contra o
/// serviço real.
/// </para>
/// <para>
/// Sem containers: <c>AddInfraDependencies</c> registra o <c>DbContext</c> sem abrir conexão, e
/// construir o <c>AmazonS3Client</c> não fala com a rede.
/// </para>
/// </remarks>
public sealed class S3ClientConfigurationTests
{
    private const string ServiceUrl = "https://garage.example.invalid";
    private const string Region = "garage";

    // Regressão (2026-09-05): o AWSSDK 4 calcula checksum de requisição por padrão
    // (WHEN_SUPPORTED) e manda o corpo do PutObject em `aws-chunked` com trailer de CRC32; o
    // Garage recusa esse formato com "Bad request: Invalid payload signature". O erro engana
    // porque parece credencial errada — mas a assinatura do CABEÇALHO passou, e o que ele
    // reprova é a do CORPO. Toda captura por e-mail ficou parada até isto ser desligado.
    [Fact]
    public void S3Client_ShouldNotCalculateRequestChecksums()
    {
        using var provider = BuildProvider();
        var client = provider.GetRequiredService<IAmazonS3>();

        // Prova que este é o cliente configurado, e não um homônimo com os defaults da AWS: sem
        // isto, um substituto entrando no lugar faria o teste afirmar sobre outra coisa.
        Assert.Equal(ServiceUrl, client.Config.ServiceURL.TrimEnd('/'));
        Assert.Equal(Region, client.Config.AuthenticationRegion);

        Assert.Equal(RequestChecksumCalculation.WHEN_REQUIRED, client.Config.RequestChecksumCalculation);
    }

    // A outra metade do mesmo padrão do SDK 4: o serviço auto-hospedado não devolve os
    // cabeçalhos de checksum que ele passou a querer validar na leitura, e validar por padrão
    // quebraria o download do documento original — o caminho que serve o comprovante a uma
    // pessoa.
    [Fact]
    public void S3Client_ShouldNotValidateResponseChecksums()
    {
        using var provider = BuildProvider();
        var client = provider.GetRequiredService<IAmazonS3>();

        Assert.Equal(ResponseChecksumValidation.WHEN_REQUIRED, client.Config.ResponseChecksumValidation);
    }

    // O balde auto-hospedado não tem DNS por balde: sem path-style, o SDK monta
    // `<bucket>.<host>` e a requisição nem chega ao serviço.
    [Fact]
    public void S3Client_ShouldUsePathStyleAddressing()
    {
        using var provider = BuildProvider();
        var client = provider.GetRequiredService<IAmazonS3>();

        // ForcePathStyle é do AmazonS3Config, não do IClientConfig — o cast é a asserção.
        var config = Assert.IsType<AmazonS3Config>(client.Config);

        Assert.True(config.ForcePathStyle);
    }

    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BillPayment"] = "Host=localhost;Database=none",
                ["Storage:ServiceUrl"] = ServiceUrl,
                ["Storage:AccessKey"] = "GKtest",
                ["Storage:SecretKey"] = "secret",
                ["Storage:AuthenticationRegion"] = Region,
            })
            .Build();

        return new ServiceCollection()
            .AddInfraDependencies(configuration)
            .BuildServiceProvider();
    }
}
