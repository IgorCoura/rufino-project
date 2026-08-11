namespace BillPayment.Infra.Secrets;

using System.Security.Cryptography;

/// <summary>
/// Configuração do cofre de segredos por tenant.
/// </summary>
/// <remarks>
/// <para>
/// <strong><see cref="MasterKey"/> nunca vai para <c>appsettings.json</c>.</strong> Produção:
/// variável de ambiente <c>Secrets__MasterKey</c> no Dokploy. Desenvolvimento e testes:
/// <c>dotnet user-secrets set "Secrets:MasterKey" "&lt;base64 de 32 bytes&gt;"</c>.
/// </para>
/// <para>
/// <strong>Guarde uma cópia cifrada fora do host.</strong> Perder esta chave é perder todos os
/// tokens OAuth e chaves de subconta de todos os clientes — recuperável reconectando caixas e
/// reemitindo chaves, mas é incidente com todo mundo ao mesmo tempo (<c>ADR-009</c>).
/// </para>
/// </remarks>
public sealed class SecretsOptions
{
    public const string SectionName = "Secrets";

    /// <summary>Tamanho exigido da master key, em bytes. AES-256.</summary>
    public const int MASTER_KEY_LENGTH = 32;

    /// <summary>Base64 de exatamente <see cref="MASTER_KEY_LENGTH"/> bytes.</summary>
    public string? MasterKey { get; set; }

    /// <summary>
    /// Versão da chave em uso, gravada em cada linha. Ao rotacionar, incremente e re-envelope
    /// os DEKs das linhas com versão anterior.
    /// </summary>
    public int KekVersion { get; set; } = 1;

    /// <summary>Devolve a chave em bytes, ou <c>null</c> quando não configurada ou malformada.</summary>
    public byte[]? ResolveMasterKey()
    {
        if (string.IsNullOrWhiteSpace(MasterKey))
            return null;

        Span<byte> buffer = stackalloc byte[MASTER_KEY_LENGTH];
        if (!Convert.TryFromBase64String(MasterKey, buffer, out var written) || written != MASTER_KEY_LENGTH)
            return null;

        return buffer.ToArray();
    }

    /// <summary>Gera uma master key nova, no formato que a variável de ambiente espera.</summary>
    public static string GenerateMasterKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(MASTER_KEY_LENGTH));
}
