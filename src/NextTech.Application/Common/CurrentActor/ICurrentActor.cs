namespace NextTech.Application.Common.CurrentActor;

/// <summary>
/// Identidad ya autenticada, leída del token.
/// Carrito, checkout y entrega usan esto; nunca toman el ID del body.
/// </summary>
public interface ICurrentActor
{
    bool IsAuthenticated { get; }

    bool EsComprador { get; }

    bool EsInterno { get; }

    long RequireIdCompradorExterno();

    int RequireIdUsuarioInterno();

    string? Rol { get; }

    string? Email { get; }

    string? Nickname { get; }

    string? Telefono { get; }
}
