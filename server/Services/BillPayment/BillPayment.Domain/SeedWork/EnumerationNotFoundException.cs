namespace BillPayment.Domain.SeedWork;

/// <summary>
/// O nome ou valor recebido não corresponde a nenhum item do Smart Enum pedido.
/// </summary>
/// <remarks>
/// Deriva de <see cref="InvalidOperationException"/> para não quebrar quem já capturava a base,
/// mas existe como tipo próprio porque a API precisa distinguir <em>input inválido</em> (400) de
/// qualquer outra <c>InvalidOperationException</c> — o EF Core lança essa mesma base para falhas
/// internas ("second operation started on this context", <c>Single()</c> vazio), e até
/// 2026-08-28 o filtro devolvia todas elas como 400 com a mensagem interna no corpo.
/// </remarks>
public sealed class EnumerationNotFoundException : InvalidOperationException
{
    public EnumerationNotFoundException()
    {
    }

    public EnumerationNotFoundException(string message) : base(message)
    {
    }

    public EnumerationNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
