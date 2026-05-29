namespace EconomicCore.Application.Mediator;

/// <summary>
/// Marker para requests que retornam <typeparamref name="TResponse"/>.
/// Todo Command e Query implementa essa interface.
/// </summary>
public interface IRequest<out TResponse>;
