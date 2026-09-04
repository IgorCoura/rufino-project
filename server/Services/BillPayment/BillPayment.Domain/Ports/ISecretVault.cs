namespace BillPayment.Domain.Ports;

using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Guarda e devolve segredos por tenant — tokens OAuth de caixa, chave da subconta do provedor,
/// credencial de portal, senha de PDF aprendida. O Domain só vê <see cref="CredentialRef"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Nada aqui commita.</strong> As operações de escrita apenas registram a intenção na
/// mesma unidade de trabalho de quem chamou; o efeito só existe depois do <c>SaveEntitiesAsync</c>
/// do handler. Isso é deliberado: guardar o segredo e criar o agregado que o referencia têm de
/// ser atômicos, senão uma falha no meio deixa credencial órfã no cofre ou agregado apontando
/// para o vazio.
/// </para>
/// <para>
/// <strong>O valor devolvido não pode ser logado.</strong> Regra do <c>ADR-009</c>, válida para
/// todo consumidor desta porta, inclusive em mensagem de exceção.
/// </para>
/// </remarks>
public interface ISecretVault
{
    /// <summary>Devolve o segredo em claro. Lança quando a referência não existe ou não decifra.</summary>
    Task<string> ResolveAsync(CredentialRef credentialRef, CancellationToken cancellationToken);

    /// <summary>Guarda um segredo novo e devolve a referência que o agregado deve gravar.</summary>
    Task<CredentialRef> StoreAsync(TenantId tenantId, SecretKind kind, string secret, CancellationToken cancellationToken);

    /// <summary>
    /// Troca o valor mantendo a mesma referência — o caso do refresh de token OAuth, que
    /// acontece com frequência e não deveria obrigar a mutar o agregado que aponta para ele.
    /// </summary>
    Task ReplaceAsync(CredentialRef credentialRef, string secret, CancellationToken cancellationToken);

    /// <summary>Apaga a credencial. Idempotente: referência inexistente não é erro.</summary>
    Task RemoveAsync(CredentialRef credentialRef, CancellationToken cancellationToken);
}
