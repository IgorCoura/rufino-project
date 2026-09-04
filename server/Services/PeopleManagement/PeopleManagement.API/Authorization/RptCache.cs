using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;

namespace PeopleManagement.API.Authorization
{
    public interface IRptCache
    {
        /// <summary>
        /// Devolve o retrato de permissões do token da requisição corrente, buscando no servidor de
        /// autorização apenas quando não há entrada válida em cache.
        /// </summary>
        Task<RptFetchResult> GetAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Cache do retrato de permissões (RPT) por token. Uma ida ao Keycloak por token, não por
    /// requisição.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>A chave é o SHA-256 do token inteiro</strong>, não o <c>sub</c>. Dois tokens da mesma
    /// pessoa podem carregar escopos diferentes (papel revogado entre um login e outro), e chavear
    /// por identidade faria um herdar as permissões do outro. O hash existe para o token cru não
    /// ficar morando em chave de dicionário, onde qualquer despejo de memória o exporia.
    /// </para>
    /// <para>
    /// <strong>O TTL nunca ultrapassa a validade do token.</strong> Passado o <c>exp</c>, a
    /// autenticação já recusa a requisição antes de a autorização ser consultada. Dentro dele vale
    /// o <see cref="AuthorizationOptions.RptCacheTtl"/>, que é a janela em que uma permissão
    /// revogada no console ainda vale — 60 s por padrão.
    /// </para>
    /// <para>
    /// <strong>Falha não é cacheada, mas o retrato anterior sobrevive a ela</strong>
    /// (<em>fail-static</em>): quando o servidor de autorização cai e existe uma entrada vencida
    /// dentro de <see cref="AuthorizationOptions.RptStaleGrace"/>, ela é servida com log em
    /// <c>Warning</c>. Indisponibilidade do Keycloak deixa de derrubar quem já estava usando o
    /// sistema. Passada a carência, volta a ser 503 — degradação silenciosa e eterna seria pior que
    /// a queda.
    /// </para>
    /// <para>
    /// Requisições simultâneas com o mesmo token compartilham UMA busca: o que entra no cache é a
    /// <see cref="Task{TResult}"/>, não o valor. Sem isso, uma tela que dispara seis chamadas ao
    /// abrir produziria seis idas ao Keycloak — exatamente o que este cache existe para evitar.
    /// </para>
    /// </remarks>
    public sealed class RptCache(
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationServerClient authorizationServerClient,
        MemoryCache cache,
        AuthorizationOptions options,
        ILogger<RptCache> logger) : IRptCache
    {
        private const string STALE_PREFIX = "rpt:stale:";
        private const string FRESH_PREFIX = "rpt:fresh:";

        public async Task<RptFetchResult> GetAsync(CancellationToken cancellationToken = default)
        {
            var token = ReadToken();

            if (string.IsNullOrEmpty(token))
                return RptFetchResult.InvalidToken();

            if (!options.RptCacheEnabled)
                return await authorizationServerClient.FetchAllPermissionsAsync(cancellationToken);

            var fingerprint = Fingerprint(token);
            var freshKey = FRESH_PREFIX + fingerprint;
            var staleKey = STALE_PREFIX + fingerprint;

            if (cache.TryGetValue(freshKey, out Task<RptFetchResult>? pending) && pending is not null)
                return await pending;

            var fetch = FetchAndStoreAsync(staleKey, cancellationToken);

            // Guarda a TAREFA: as requisições que chegarem enquanto esta ainda corre esperam por
            // ela, em vez de abrirem cada uma a sua ida ao servidor de autorização.
            // A tarefa e guardada de PROPOSITO sem await aqui: e ela, e nao o valor, que vai
            // para o cache — quem chegar durante a busca espera a MESMA. O resultado e aguardado
            // logo abaixo, na mesma chamada.
    #pragma warning disable CS4014
            cache.Set(freshKey, fetch, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ResolveTtl(),
                Size = 1,
            });

    #pragma warning restore CS4014

            var result = await fetch;

            // Falha não fica cacheada: a próxima requisição tenta de novo em vez de esperar o TTL.
            if (result.Outcome != RptFetchOutcome.Resolved)
            {
                cache.Remove(freshKey);
                return ServeStaleIfPossible(staleKey, result);
            }

            return result;
        }

        private async Task<RptFetchResult> FetchAndStoreAsync(string staleKey, CancellationToken cancellationToken)
        {
            var result = await authorizationServerClient.FetchAllPermissionsAsync(cancellationToken);

            // Carencia zero DESLIGA o fail-static: nao guarda copia nenhuma, e indisponibilidade
            // volta a ser 503 imediato. Guardar com expiracao zero nao e "guardar por zero tempo"
            // — o MemoryCache recusa o valor.
            if (result.Outcome == RptFetchOutcome.Resolved && options.RptStaleGrace > TimeSpan.Zero)
            {
                cache.Set(staleKey, result.Snapshot, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = options.RptStaleGrace,
                    Size = 1,
                });
            }

            return result;
        }

        private RptFetchResult ServeStaleIfPossible(string staleKey, RptFetchResult failure)
        {
            // Token recusado NÃO é servido por retrato velho: um token revogado tem que parar de
            // valer na hora, e é a única negativa que o cliente resolve sozinho (renovando).
            if (failure.Outcome == RptFetchOutcome.InvalidToken)
                return failure;

            if (!cache.TryGetValue(staleKey, out RptSnapshot? stale) || stale is null)
                return failure;

            logger.LogWarning(
                "Servidor de autorizacao indisponivel: servindo o retrato de permissoes anterior por ate {StaleGrace}. " +
                "Permissao alterada no realm nao vale enquanto isto durar.",
                options.RptStaleGrace);

            return RptFetchResult.Resolved(stale);
        }

        /// <summary>
        /// O menor entre o TTL configurado e o que resta do token. Sem <c>exp</c> legível, vale o
        /// configurado — errar para o lado curto é o lado seguro.
        /// </summary>
        private TimeSpan ResolveTtl()
        {
            var expiration = httpContextAccessor.HttpContext?.User.FindFirst("exp")?.Value;

            if (!long.TryParse(expiration, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epochSeconds))
                return options.RptCacheTtl;

            var remaining = DateTimeOffset.FromUnixTimeSeconds(epochSeconds) - DateTimeOffset.UtcNow;

            if (remaining <= TimeSpan.Zero)
                return TimeSpan.FromSeconds(1);

            return remaining < options.RptCacheTtl ? remaining : options.RptCacheTtl;
        }

        private string ReadToken()
        {
            var header = httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Authorization].ToString();

            return string.IsNullOrWhiteSpace(header)
                ? string.Empty
                : header.Replace(options.SourceAuthenticationScheme, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        }

        private static string Fingerprint(string token)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
