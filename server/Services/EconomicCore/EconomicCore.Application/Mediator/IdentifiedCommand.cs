namespace EconomicCore.Application.Mediator;

/// <summary>
/// Embrulha um Command <typeparamref name="TCommand"/> com o <see cref="Id"/> de
/// idempotência (header <c>x-requestid</c>). Se o mesmo Id chegar duas vezes, o
/// <see cref="IdentifiedCommandHandler{TCommand,TResult}"/> devolve resposta
/// neutra em vez de reprocessar.
/// </summary>
public sealed record IdentifiedCommand<TCommand, TResult>(TCommand Command, Guid Id) : IRequest<TResult>
    where TCommand : IRequest<TResult>;
