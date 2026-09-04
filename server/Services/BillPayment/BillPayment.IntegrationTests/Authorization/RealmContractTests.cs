namespace BillPayment.IntegrationTests.Authorization;

using System.Text.Json;
using BillPayment.API.Authorization;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Liga o código ao realm: todo par <c>(recurso, escopo)</c> que um <c>[ProtectedResource]</c>
/// declara TEM que existir no <c>bill-payment-authz-config.json</c> versionado.
/// </summary>
/// <remarks>
/// <para>
/// É a cura dos achados A3 e A4 da auditoria de 2026-09-04. O sintoma era invisível no build e
/// caro em produção: recurso que o código pede e o realm não tem faz o servidor de autorização
/// responder não-2xx, o cliente traduz para negativa, e <strong>todo mundo — inclusive o
/// administrador — toma 403 naquele endpoint</strong>. Foi o que aconteceu com <c>archive</c> e
/// <c>archive-category</c> no PeopleManagement, e com o <c>"Archive"</c> escrito com A maiúsculo:
/// nome de recurso UMA é <em>case-sensitive</em>.
/// </para>
/// <para>
/// O arquivo versionado é a entrada do passo de deploy que importa a configuração no realm. Este
/// teste é o que o obriga a parar de mentir — sem ele, o arquivo diverge do código e ninguém
/// descobre até alguém reimportá-lo e quebrar metade da API.
/// </para>
/// <para>
/// O que ele NÃO prova: que o realm em produção foi de fato atualizado. Isso é passo de deploy.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class RealmContractTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string AUTHZ_CONFIG_PATH = "KeycloakConfig/bill-payment-authz-config.json";

    private static Dictionary<string, HashSet<string>> DeclaredInRealm()
    {
        Assert.True(File.Exists(AUTHZ_CONFIG_PATH),
            $"O arquivo de authz do realm não veio para o output do teste ({AUTHZ_CONFIG_PATH}). Confira o <Content Include> do csproj.");

        using var document = JsonDocument.Parse(File.ReadAllText(AUTHZ_CONFIG_PATH));

        var declared = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var resource in document.RootElement.GetProperty("resources").EnumerateArray())
        {
            var name = resource.GetProperty("name").GetString()!;
            var scopes = new HashSet<string>(StringComparer.Ordinal);

            if (resource.TryGetProperty("scopes", out var scopeArray))
            {
                foreach (var scope in scopeArray.EnumerateArray())
                    scopes.Add(scope.GetProperty("name").GetString()!);
            }

            declared[name] = scopes;
        }

        return declared;
    }

    private List<ProtectedResourceAttribute> DeclaredInCode()
        => Factory.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .Select(e => e.Metadata.GetMetadata<ProtectedResourceAttribute>())
            .Where(attr => attr is not null)
            .Select(attr => attr!)
            .ToList();

    // A varredura tem que achar atributo e o arquivo tem que ter recurso: se qualquer um dos dois
    // vier vazio, os testes abaixo passam sem provar nada.
    [Fact]
    public void OInventarioDosDoisLados_NaoPodeVirVazio()
    {
        Assert.NotEmpty(DeclaredInCode());
        Assert.NotEmpty(DeclaredInRealm());
    }

    // Todo recurso citado no código existe no realm — com a MESMA caixa, porque nome de recurso
    // UMA é case-sensitive e um "Bill" no lugar de "bill" nega tudo em silêncio.
    [Fact]
    public void TodoRecursoDoCodigo_TemQueExistirNoRealm()
    {
        var realm = DeclaredInRealm();

        var missing = DeclaredInCode()
            .Select(attr => attr.Resource)
            .Distinct(StringComparer.Ordinal)
            .Where(resource => !realm.ContainsKey(resource))
            .OrderBy(resource => resource, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missing.Count == 0,
            "Recursos usados em [ProtectedResource] e ausentes do authz-config do realm "
            + $"(todo endpoint que os cite responde 403 para TODO mundo): {string.Join(", ", missing)}");
    }

    // Todo escopo citado no código existe no recurso correspondente do realm.
    [Fact]
    public void TodoEscopoDoCodigo_TemQueExistirNoRecursoDoRealm()
    {
        var realm = DeclaredInRealm();

        var missing = DeclaredInCode()
            .SelectMany(attr => attr.Scopes.Select(scope => (attr.Resource, Scope: scope)))
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Scope))
            .Distinct()
            .Where(pair => !realm.TryGetValue(pair.Resource, out var scopes) || !scopes.Contains(pair.Scope))
            .Select(pair => $"{pair.Resource}#{pair.Scope}")
            .OrderBy(pair => pair, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"Pares recurso#escopo usados no código e ausentes do authz-config do realm: {string.Join(", ", missing)}");
    }

    // O caminho inverso: recurso declarado no realm que nenhum endpoint usa é configuração morta —
    // superfície de permissão que ninguém revisa. Não é erro fatal do mesmo jeito, mas tem que ser
    // decisão explícita.
    [Fact]
    public void TodoRecursoDoRealm_TemQueSerUsadoPorAlgumEndpoint()
    {
        var used = DeclaredInCode().Select(attr => attr.Resource).ToHashSet(StringComparer.Ordinal);

        var unused = DeclaredInRealm().Keys
            .Where(resource => !string.Equals(resource, "Default Resource", StringComparison.Ordinal))
            .Where(resource => !used.Contains(resource))
            .OrderBy(resource => resource, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unused.Count == 0,
            $"Recursos declarados no realm que nenhum endpoint usa (configuração morta): {string.Join(", ", unused)}");
    }

    // Nenhuma descrição passa de 255 caracteres — o limite das colunas do Keycloak.
    //
    // Não é preciosismo: `KEYCLOAK_ROLE.DESCRIPTION` e as colunas equivalentes de policy e
    // recurso são `varchar(255)`, e o import morre com
    // `ERROR: value too long for type character varying(255)` — **derrubando o arranque inteiro
    // do Keycloak**, não só aquele registro. Aconteceu em 2026-09-04 com a descrição do papel
    // `developer`, e o realm local ficou sem subir. O texto longo pertence ao CONVENCOES.md e aos
    // comentários do código; aqui cabe a frase que aparece no console.
    [Fact]
    public void NenhumaDescricaoDoRealm_PodePassarDoLimiteDaColuna()
    {
        const int LIMITE = 255;

        Assert.True(File.Exists(AUTHZ_CONFIG_PATH),
            $"O arquivo de authz do realm não veio para o output do teste ({AUTHZ_CONFIG_PATH}).");

        using var document = JsonDocument.Parse(File.ReadAllText(AUTHZ_CONFIG_PATH));

        var longos = new List<string>();

        void Percorrer(JsonElement node, string caminho)
        {
            switch (node.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var propriedade in node.EnumerateObject())
                    {
                        var filho = $"{caminho}/{propriedade.Name}";

                        if (propriedade.Value.ValueKind == JsonValueKind.String
                            && propriedade.Name is "description" or "displayName" or "name"
                            && propriedade.Value.GetString()!.Length > LIMITE)
                        {
                            longos.Add($"{filho} ({propriedade.Value.GetString()!.Length} caracteres)");
                        }

                        Percorrer(propriedade.Value, filho);
                    }

                    break;

                case JsonValueKind.Array:
                    var indice = 0;
                    foreach (var item in node.EnumerateArray())
                        Percorrer(item, $"{caminho}[{indice++}]");

                    break;
            }
        }

        Percorrer(document.RootElement, string.Empty);

        Assert.True(
            longos.Count == 0,
            $"Campos acima de {LIMITE} caracteres — o import do realm falha e o Keycloak não sobe: {string.Join(", ", longos)}");
    }

}
