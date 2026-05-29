namespace EconomicCore.Application.Mediator;

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Mediator próprio (substituto do MediatR). Resolve o handler e os pipeline
/// behaviors via DI e compõe a cadeia por delegate chaining. Cacheia um wrapper
/// por tipo de request — a primeira chamada paga reflection, as demais são O(1).
/// </summary>
public sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> _handlerCache = new();

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (IRequestHandlerWrapper<TResponse>)_handlerCache.GetOrAdd(
            request.GetType(),
            static requestType =>
            {
                var responseType = requestType.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                    .GetGenericArguments()[0];

                var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);
                return Activator.CreateInstance(wrapperType)!;
            });

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }

    private interface IRequestHandlerWrapper<TResponse>
    {
        Task<TResponse> Handle(
            IRequest<TResponse> request,
            IServiceProvider provider,
            CancellationToken ct);
    }

    private sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : IRequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(
            IRequest<TResponse> request,
            IServiceProvider provider,
            CancellationToken ct)
        {
            var handler = provider.GetService<IRequestHandler<TRequest, TResponse>>()
                ?? throw new InvalidOperationException(
                    $"Handler para {typeof(TRequest).Name} não foi registrado.");

            // Reverte para que o primeiro registrado seja o mais externo da cadeia.
            var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse();

            // `t == default ? ct : t`: cada elo usa o token original a menos que um behavior
            // passe um token derivado adiante (mesma semântica do MediatR).
            RequestHandlerDelegate<TResponse> pipeline =
                t => handler.Handle((TRequest)request, t == default ? ct : t);

            foreach (var behavior in behaviors)
            {
                var next = pipeline;
                pipeline = t => behavior.Handle((TRequest)request, next, t == default ? ct : t);
            }

            return pipeline(ct);
        }
    }
}
