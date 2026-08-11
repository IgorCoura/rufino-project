namespace BillPayment.Application.Queries;

/// <summary>
/// Codifica e decodifica o cursor de paginação por keyset — base64 dos <c>Ticks</c> do
/// <c>CreatedAt</c> da última linha da página.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Keyset por <c>CreatedAt</c>, não por <c>Id</c>.</strong> O Id é value-converted e o
/// EF não traduz comparação de ordem sobre ele; <c>CreatedAt</c> é <c>DateTime</c> e traduz
/// direto. Toda lista do BC pagina assim.
/// </para>
/// <para>
/// <strong>Cursor inválido não é erro.</strong> <see cref="TryDecode"/> devolve <c>false</c> e a
/// lista começa do início — um cursor corrompido, expirado ou forjado degrada para a primeira
/// página em vez de derrubar a tela com 500.
/// </para>
/// <para>
/// Isto vivia copiado, byte a byte, dentro de cada <c>XxxQueries</c>. Uma cópia por lista
/// significaria que trocar a codificação exigiria achar todas — e a que ficasse para trás
/// devolveria página errada em silêncio, que é o pior modo de falhar numa paginação.
/// </para>
/// </remarks>
internal static class CursorCodec
{
    public static string Encode(DateTime createdAt)
        => Convert.ToBase64String(BitConverter.GetBytes(createdAt.Ticks));

    public static bool TryDecode(string? cursor, out DateTime createdAt)
    {
        createdAt = default;
        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(cursor);
            if (bytes.Length != sizeof(long))
                return false;

            var ticks = BitConverter.ToInt64(bytes);
            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                return false;

            createdAt = new DateTime(ticks, DateTimeKind.Utc);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
