namespace BillPayment.Domain.Secrets;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Que natureza de segredo por tenant está guardada. Não é rótulo decorativo: o tipo entra no
/// dado autenticado da cifra (AAD), então um segredo gravado como token de caixa não decifra
/// se alguém o apresentar como chave de subconta.
/// </summary>
/// <remarks>
/// O catálogo é o do <c>ADR-009</c>. Só a chave de subconta Asaas é usada na Fase 1; as demais
/// entram com a captura (fase 2) e o pagamento (fase 3). Estão declaradas desde já porque o
/// tipo é gravado na linha cifrada — acrescentar um valor depois é barato, mudar o significado
/// de um valor já gravado não é.
/// </remarks>
public sealed class SecretKind : Enumeration
{
    /// <summary>Chave de API da subconta Asaas do tenant. Capaz de consultar e de pagar.</summary>
    public static readonly SecretKind AsaasAccountApiKey = new(1, "AsaasAccountApiKey");

    /// <summary>Refresh token OAuth da caixa de e-mail monitorada.</summary>
    public static readonly SecretKind MailboxOAuthToken = new(2, "MailboxOAuthToken");

    /// <summary>Credencial de acesso a portal de fornecedor.</summary>
    public static readonly SecretKind PortalCredential = new(3, "PortalCredential");

    /// <summary>Senha de PDF aprendida. Nunca é logada nem devolvida por API.</summary>
    public static readonly SecretKind PdfPassword = new(4, "PdfPassword");

    private SecretKind(int id, string name) : base(id, name) { }
}
