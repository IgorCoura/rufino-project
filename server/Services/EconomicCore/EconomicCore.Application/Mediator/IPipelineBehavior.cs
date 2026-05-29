namespace EconomicCore.Application.Mediator;

/// <summary>
/// Envolve a execução de um handler para cross-cutting concerns (logging,
/// validação, transação). A ordem de execução segue a ordem de registro no DI:
/// o primeiro registrado é o mais externo.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
