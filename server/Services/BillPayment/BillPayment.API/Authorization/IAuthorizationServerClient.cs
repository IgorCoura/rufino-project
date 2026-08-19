namespace BillPayment.API.Authorization;

/// <summary>Desfecho de uma checagem de permissão UMA contra o servidor de autorização.</summary>
public enum ResourceAccessResult
{
    /// <summary>O token vale e concede o recurso/escopos pedidos.</summary>
    Granted,

    /// <summary>O token vale, mas não concede o recurso/escopos pedidos.</summary>
    Denied,

    /// <summary>O servidor de autorização recusou o próprio token (expirado, revogado, not-before).</summary>
    InvalidToken,

    /// <summary>O servidor de autorização não foi alcançado ou respondeu com erro de servidor.</summary>
    ServerUnavailable,
}

public interface IAuthorizationServerClient
{
    Task<ResourceAccessResult> VerifyAccessToResouce(string permission, CancellationToken cancellationToken = default);
}
