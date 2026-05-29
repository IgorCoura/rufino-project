namespace EconomicCore.Application.Mediator;

/// <summary>
/// Próximo elo da cadeia de pipeline behaviors. Quando invocado, executa o
/// próximo behavior — ou o handler final, se não houver mais behaviors. O token
/// é opcional: passar <c>default</c> faz o elo usar o token original do request;
/// passar um token derivado (ex.: linked/timeout) propaga-o ao restante da cadeia.
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);
