namespace BillPayment.Infra.Extraction.Links;

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

/// <summary>
/// A última barreira antes de o sistema abrir conexão para um endereço que veio de fora.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Devolve o IP, e é isso que muda tudo.</strong> A versão anterior respondia
/// <c>bool</c> e entregava o <em>nome</em> de volta ao <c>HttpClient</c>, que resolvia outra vez
/// na hora de conectar. Entre as duas resoluções o DNS pode responder outra coisa — é o ataque de
/// rebinding, e ele derrota qualquer conferência feita sobre o nome. Devolvendo o endereço já
/// conferido, quem chama disca <em>nele</em>, e a segunda resolução deixa de existir.
/// </para>
/// <para>
/// <strong>Reprova se QUALQUER endereço do host for interno</strong>, não se todos forem. Um host
/// que resolve para um endereço público e um privado é exatamente a forma que o rebinding assume;
/// aceitar o público seria aceitar a chance de a conexão escolher o outro.
/// </para>
/// <para>
/// <strong>As faixas reservadas são código, não configuração.</strong> Uma faixa esquecida é um
/// buraco de SSRF, e configuração é onde as coisas são esquecidas. O que é configurável são as
/// faixas que só a instalação conhece — os endereços públicos da própria VPS, que precisam ser
/// inalcançáveis a partir daqui e que nenhum código teria como adivinhar.
/// </para>
/// </remarks>
internal sealed class SafeUrlPolicy
{
    private readonly CidrRange[] _blocked;
    private readonly CidrRange[] _allowed;

    public SafeUrlPolicy(IOptions<LinkResolutionOptions> options)
    {
        var value = options.Value;

        _blocked = Parse(value.BlockedCidrs, nameof(LinkResolutionOptions.BlockedCidrs));
        _allowed = Parse(value.AllowedCidrs, nameof(LinkResolutionOptions.AllowedCidrs));
    }

    /// <summary>
    /// Resolve o host e devolve o endereço no qual é seguro discar, ou <c>null</c>.
    /// </summary>
    public async Task<IPAddress?> ResolvePinnedAddressAsync(string host, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        // Endereço literal não passa por DNS, e é a forma mais direta de tentar alcançar a rede
        // interna — a conferência é a mesma, e não há o que pinar além dele próprio.
        if (IPAddress.TryParse(host.Trim('[', ']'), out var literal))
            return IsAllowed(literal) ? literal : null;

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);

            if (addresses.Length == 0 || !Array.TrueForAll(addresses, IsAllowed))
                return null;

            // IPv4 primeiro: em contêiner com IPv6 sem rota de saída, discar no AAAA falha por
            // timeout — e um timeout aqui é indistinguível de host fora do ar.
            return Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork)
                ?? addresses[0];
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            // Host que não resolve não é alcançável; recusar é o mesmo desfecho de não achar.
            return null;
        }
    }

    /// <summary>
    /// Se é permitido abrir conexão para este endereço.
    /// </summary>
    /// <remarks>
    /// A exceção configurada vem primeiro de propósito: toda faixa que alguém precisa liberar
    /// está proibida por algum motivo, senão não precisaria ser liberada — checar a proibição
    /// antes tornaria a lista de exceções inútil.
    /// </remarks>
    public bool IsAllowed(IPAddress address)
    {
        if (Array.Exists(_allowed, r => r.Contains(address)))
            return true;

        if (Array.Exists(_blocked, r => r.Contains(address)))
            return false;

        return IsPubliclyRoutable(address);
    }

    private static CidrRange[] Parse(IList<string> values, string fieldName)
    {
        if (values is null || values.Count == 0)
            return [];

        var ranges = new List<CidrRange>(values.Count);

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            // Estourar no arranque, e não ignorar em silêncio: uma faixa que ninguém percebeu que
            // não vale é pior que faixa nenhuma — quem a escreveu acredita estar protegido.
            if (!CidrRange.TryParse(value, out var range))
            {
                throw new InvalidOperationException(
                    $"{LinkResolutionOptions.SectionName}:{fieldName} tem uma faixa inválida: '{value}'. "
                    + "Use CIDR (10.0.0.0/8) ou um endereço solto (203.0.113.10).");
            }

            ranges.Add(range);
        }

        return [.. ranges];
    }

    private static bool IsPubliclyRoutable(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return false;

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPublicV4(address.GetAddressBytes()),
            AddressFamily.InterNetworkV6 => IsPublicV6(address),
            _ => false,
        };
    }

    private static bool IsPublicV4(byte[] octets) => octets[0] switch
    {
        0 => false,                                        // "este host", nesta rede
        10 => false,                                       // RFC 1918
        127 => false,                                      // loopback
        100 when octets[1] is >= 64 and <= 127 => false,   // CGNAT (RFC 6598)
        169 when octets[1] == 254 => false,                // link-local — inclui metadados de nuvem
        172 when octets[1] is >= 16 and <= 31 => false,    // RFC 1918
        192 when octets[1] == 0 && octets[2] == 0 => false,       // atribuições do IETF
        192 when octets[1] == 0 && octets[2] == 2 => false,       // TEST-NET-1
        192 when octets[1] == 88 && octets[2] == 99 => false,     // relay anycast 6to4
        192 when octets[1] == 168 => false,                // RFC 1918
        198 when octets[1] is 18 or 19 => false,           // benchmarking (RFC 2544)
        198 when octets[1] == 51 && octets[2] == 100 => false,    // TEST-NET-2
        203 when octets[1] == 0 && octets[2] == 113 => false,     // TEST-NET-3
        >= 224 => false,                                   // multicast e reservado
        _ => true,
    };

    /// <summary>
    /// IPv6, incluindo as três formas que a versão anterior deixava passar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong><c>::</c> passava como público.</strong> <c>IPAddress.IsLoopback</c> compara com
    /// <c>::1</c> exato, <c>fc00::/7</c> não casa com zeros, e <c>IsIPv4MappedToIPv6</c> exige
    /// <c>0xFFFF</c> nos bytes 10-11 — o endereço não-especificado escapava das três, e
    /// <c>http://[::]/</c> chega no loopback na maioria dos stacks.
    /// </para>
    /// <para>
    /// <strong><c>::127.0.0.1</c> também.</strong> É o IPv4-<em>compatible</em> (RFC 4291,
    /// deprecado), primo do IPv4-<em>mapped</em>: mesmos zeros, sem o <c>0xFFFF</c>. Aqui os dois
    /// são desembrulhados e conferidos como o IPv4 que eles carregam.
    /// </para>
    /// <para>
    /// <strong>NAT64 e 6to4 embutem IPv4 e por isso são recusados por inteiro.</strong>
    /// <c>64:ff9b::/96</c> e <c>2002::/16</c> traduzem para um endereço v4 que pode ser interno, e
    /// conferir o v4 embutido dependeria de a tradução ser a que se espera — que é decisão de um
    /// gateway que não é nosso.
    /// </para>
    /// </remarks>
    private static bool IsPublicV6(IPAddress address)
    {
        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
            return false;

        var bytes = address.GetAddressBytes();

        // fc00::/7 — endereço local único, o equivalente v6 da faixa privada.
        if ((bytes[0] & 0xFE) == 0xFC)
            return false;

        // 2002::/16 — 6to4, carrega um IPv4 nos bytes 2-5.
        if (bytes[0] == 0x20 && bytes[1] == 0x02)
            return false;

        // 64:ff9b::/96 e 64:ff9b:1::/48 — NAT64.
        if (bytes[0] == 0x00 && bytes[1] == 0x64 && bytes[2] == 0xFF && bytes[3] == 0x9B)
            return false;

        // Bytes 0-9 zerados: ::, ::1, IPv4-compatible (::a.b.c.d) e IPv4-mapped (::ffff:a.b.c.d).
        if (bytes.AsSpan(0, 10).IndexOfAnyExcept((byte)0) < 0)
        {
            var isMapped = bytes[10] == 0xFF && bytes[11] == 0xFF;
            var isCompatible = bytes[10] == 0x00 && bytes[11] == 0x00;

            if (!isMapped && !isCompatible)
                return false;

            // :: (não-especificado) não tem IPv4 dentro e alcança loopback na maioria dos stacks.
            // ::1 cai no IsPublicV4 abaixo, que recusa 127/8 inteiro junto com 0/8.
            var embedded = bytes.AsSpan(12, 4);

            return embedded.IndexOfAnyExcept((byte)0) >= 0 && IsPublicV4(embedded.ToArray());
        }

        return true;
    }
}
