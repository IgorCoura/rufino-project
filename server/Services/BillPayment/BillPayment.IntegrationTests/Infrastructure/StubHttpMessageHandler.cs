namespace BillPayment.IntegrationTests.Infrastructure;

using System.Net;
using System.Text;

/// <summary>
/// Transporte de teste para os adapters do provedor.
/// </summary>
/// <remarks>
/// <para>
/// A regra da suíte é não mockar a dependência sob teste — e aqui ela é respeitada: o que está
/// sob teste é a <strong>tradução</strong> entre a resposta do provedor e os VOs do domínio, e
/// é exatamente ela que roda de verdade. O que é substituído é o transporte, porque a
/// alternativa seria a suíte carregar uma credencial capaz de pagar contas e depender de um
/// serviço externo pago para dizer se um mapeamento está certo.
/// </para>
/// <para>
/// Guarda a última requisição para que os testes possam verificar o que foi enviado — é assim
/// que se prova, por exemplo, que a data prevista de pagamento chega ao decode do Pix.
/// </para>
/// </remarks>
internal sealed class StubHttpMessageHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public string? LastRequestBody { get; private set; }
    public Uri? LastRequestUri { get; private set; }

    /// <summary>
    /// Os cabeçalhos da última requisição — é como se prova que a chave DO TENANT viajou no
    /// <c>access_token</c> (a chave por tenant é o ponto da mudança de 2026-08-31).
    /// </summary>
    public Dictionary<string, string> LastRequestHeaders { get; } = [];

    /// <summary>Quantas requisições saíram — zero prova que a degradação não tocou a rede.</summary>
    public int RequestCount { get; private set; }

    /// <summary>Falha de transporte, para exercitar o caminho de indisponibilidade.</summary>
    public Exception? ThrowInstead { get; init; }

    public static StubHttpMessageHandler Ok(string body) => new(HttpStatusCode.OK, body);

    public static StubHttpMessageHandler Error(HttpStatusCode status, string code, string description)
        => new(status, $$"""{"errors":[{"code":"{{code}}","description":"{{description}}"}]}""");

    public static StubHttpMessageHandler Throwing(Exception exception)
        => new(HttpStatusCode.OK, string.Empty) { ThrowInstead = exception };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        RequestCount++;

        LastRequestHeaders.Clear();
        foreach (var header in request.Headers)
            LastRequestHeaders[header.Key] = string.Join(",", header.Value);

        if (ThrowInstead is not null)
            throw ThrowInstead;

        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }
}

/// <summary>
/// Relógio parado. Os adapters carimbam o instante da consulta, e o teste precisa poder afirmar
/// qual instante foi gravado.
/// </summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
