namespace BillPayment.Domain.Lookups;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Como terminou a tentativa de consultar o documento na fonte oficial.
/// </summary>
/// <remarks>
/// <para>
/// A distinção entre <see cref="Unresolved"/> e <see cref="Unavailable"/> é a razão de este
/// tipo existir. "O provedor respondeu que não conhece este título" é um <strong>fato sobre o
/// documento</strong> e não melhora com retentativa; "o provedor não respondeu" é um fato
/// sobre a <strong>infraestrutura</strong> e melhora. Colapsar os dois em uma exceção só faria
/// a verificação tratar indisponibilidade de rede como suspeita do boleto.
/// </para>
/// <para>
/// A medição da sprint 1.0 mostrou que <see cref="Unresolved"/> é o caso comum, não o
/// excepcional: nenhuma das doze linhas de cobrança do corpus resolveu em sandbox
/// (ver <c>12-official-lookup-coverage.md</c>). Fluxo normal não é modelado com exceção.
/// </para>
/// </remarks>
public sealed class LookupStatus : Enumeration
{
    /// <summary>O provedor devolveu os dados do documento.</summary>
    public static readonly LookupStatus Resolved = new(1, "Resolved", hasSnapshot: true, isRetryable: false);

    /// <summary>
    /// O provedor respondeu, mas não tem o que devolver — título não registrado, código
    /// inválido para ele, natureza que ele não consulta. Retentar dá a mesma resposta.
    /// </summary>
    public static readonly LookupStatus Unresolved = new(2, "Unresolved", hasSnapshot: false, isRetryable: false);

    /// <summary>
    /// Não houve resposta útil: timeout, 5xx, circuito aberto, credencial ausente. Nada foi
    /// aprendido sobre o documento — o check correspondente sai inconclusivo, nunca reprovado.
    /// </summary>
    public static readonly LookupStatus Unavailable = new(3, "Unavailable", hasSnapshot: false, isRetryable: true);

    /// <summary>Só <see cref="Resolved"/> carrega retrato; os demais carregam motivo.</summary>
    public bool HasSnapshot { get; }

    /// <summary>Vale a pena consultar de novo mais tarde?</summary>
    public bool IsRetryable { get; }

    private LookupStatus(int id, string name, bool hasSnapshot, bool isRetryable) : base(id, name)
    {
        HasSnapshot = hasSnapshot;
        IsRetryable = isRetryable;
    }
}
