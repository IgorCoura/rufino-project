namespace BillPayment.Domain.Lookups;

/// <summary>
/// Motivos de falha de consulta que o <strong>domínio</strong> precisa reconhecer pelo nome.
/// </summary>
/// <remarks>
/// A maioria dos motivos que um adapter produz é opaca para o domínio — ele só distingue
/// resolvido de não resolvido, e retentável de definitivo. Estes não: a verificação 3 decide o
/// aviso que chega a quem aprova a partir deles, e um aviso que promete "revalide mais tarde"
/// sobre uma conta sem chave configurada seria mentira. Ficam aqui, e não na Infra, porque quem
/// os lê é o domínio; o adapter os importa.
/// </remarks>
public static class LookupReasons
{
    /// <summary>
    /// O tenant ainda não vinculou a chave do provedor. <strong>Revalidar não resolve</strong> —
    /// depende de um cadastro, não do tempo.
    /// </summary>
    public const string TENANT_KEY_NOT_CONFIGURED = "tenant_key_not_configured";
}
