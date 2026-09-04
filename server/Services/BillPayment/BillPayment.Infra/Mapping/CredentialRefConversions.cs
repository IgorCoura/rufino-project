namespace BillPayment.Infra.Mapping;

using BillPayment.Domain.Secrets;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// <see cref="CredentialRef"/> vira <strong>uma coluna de texto</strong> na forma canônica
/// <c>esquema:chave</c> — o mesmo motivo do <see cref="TaxIdConversions"/>: owned type não é
/// endereçável a partir da raiz e inviabilizaria índice sobre a coluna.
/// </summary>
/// <remarks>
/// A rehidratação passa por <see cref="CredentialRef.Parse"/>, então uma referência corrompida
/// falha alto na leitura. Isso é desejável: um ponteiro de cofre ilegível precisa aparecer como
/// erro, não virar credencial silenciosamente nula que faria a fonte "só parar de sincronizar".
/// <para>
/// <strong>A coluna guarda o ponteiro, jamais o segredo</strong> — o valor cifrado vive em
/// <c>tenant_secrets</c> e só o <c>ISecretVault</c> o toca.
/// </para>
/// </remarks>
internal static class CredentialRefConversions
{
    public const int MAX_LENGTH = CredentialRef.MAX_LENGTH;

    /// <summary>
    /// Tipado como anulável porque o único consumidor hoje é <c>CaptureSource.Credential</c>,
    /// que é opcional por Kind. O <c>!</c> é seguro: o EF nunca chama o conversor para
    /// <c>null</c> — coluna nula vira propriedade nula sem passar por aqui.
    /// </summary>
    public static readonly ValueConverter<CredentialRef?, string> Single =
        new(credential => credential!.ToString(), value => CredentialRef.Parse(value));

    public static readonly ValueComparer<CredentialRef> SingleComparer =
        new((left, right) => left == null ? right == null : left.Equals(right),
            credential => credential.GetHashCode(),
            credential => credential);
}
