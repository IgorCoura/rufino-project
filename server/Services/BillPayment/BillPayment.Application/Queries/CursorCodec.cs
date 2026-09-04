namespace BillPayment.Application.Queries;

/// <summary>
/// Codifica e decodifica o cursor de paginação por keyset — <c>CreatedAt</c> <strong>e</strong>
/// <c>Id</c> da última linha da página.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O <c>Id</c> entra no cursor porque <c>CreatedAt</c> não é único — e o empate não é
/// raro, é o caso normal.</strong> Uma varredura de caixa carimba um instante só e o repassa a
/// todos os itens que ingere: medido em produção em 2026-08-11, 404 itens com
/// <c>count(DISTINCT created_at) = 1</c>. Com o cursor carregando só a data, a página 2 filtrava
/// <c>CreatedAt &gt; T</c> e voltava <strong>vazia</strong>, tornando inalcançável tudo além da
/// primeira página — sem erro, sem log, com a lista afirmando que acabou.
/// </para>
/// <para>
/// <strong>A ordem do desempate tem que acompanhar a da chave primária.</strong> Lista ascendente
/// desempata <c>Id</c> ascendente e filtra <c>&gt;</c>; descendente desempata descendente e filtra
/// <c>&lt;</c>. Cruzar as direções (o <c>BillQueries</c> cruzava) reintroduz o mesmo buraco: o
/// <c>ORDER BY</c> e o <c>WHERE</c> passam a discordar sobre quem já foi visto.
/// </para>
/// <para>
/// <strong>Cursor inválido não é erro.</strong> <see cref="TryDecode"/> devolve <c>false</c> e a
/// lista começa do início — corrompido, forjado, ou no formato antigo de 8 bytes, degrada para a
/// primeira página em vez de derrubar a tela com 500.
/// </para>
/// <para>
/// Isto vivia copiado, byte a byte, dentro de cada <c>XxxQueries</c>. Uma cópia por lista
/// significaria que trocar a codificação exigiria achar todas — e a que ficasse para trás
/// devolveria página errada em silêncio, que é o pior modo de falhar numa paginação. Foi
/// exatamente o que aconteceu com o desempate.
/// </para>
/// </remarks>
internal static class CursorCodec
{
    private const int GUID_LENGTH = 16;
    private const int PAYLOAD_LENGTH = sizeof(long) + GUID_LENGTH;

    public static string Encode(DateTime createdAt, Guid id)
    {
        Span<byte> buffer = stackalloc byte[PAYLOAD_LENGTH];

        BitConverter.TryWriteBytes(buffer, createdAt.Ticks);
        id.TryWriteBytes(buffer[sizeof(long)..]);

        return Convert.ToBase64String(buffer);
    }

    public static bool TryDecode(string? cursor, out DateTime createdAt, out Guid id)
    {
        createdAt = default;
        id = Guid.Empty;

        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(cursor);

            // Cursor no formato antigo (só a data) cai aqui e reinicia a lista. É o desfecho
            // certo: honrá-lo pela metade devolveria página com item repetido ou faltando.
            if (bytes.Length != PAYLOAD_LENGTH)
                return false;

            var ticks = BitConverter.ToInt64(bytes);
            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                return false;

            createdAt = new DateTime(ticks, DateTimeKind.Utc);
            id = new Guid(bytes[sizeof(long)..]);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
