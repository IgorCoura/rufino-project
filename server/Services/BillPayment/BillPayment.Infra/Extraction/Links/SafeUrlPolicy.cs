namespace BillPayment.Infra.Extraction.Links;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// A última barreira antes de o sistema buscar um endereço que veio de fora.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A allowlist sozinha não basta.</strong> Ela decide sobre o <em>nome</em>, e nome é
/// resolvido por DNS que não é nosso: um host autorizado cujo registro passe a apontar para
/// <c>127.0.0.1</c> ou <c>169.254.169.254</c> transformaria a escada de resolução num canal para
/// alcançar serviços internos e metadados de nuvem a partir de um e-mail. Conferir o endereço
/// resolvido é o que fecha essa porta.
/// </para>
/// <para>
/// <strong>Sem seguir redirecionamento, e isso é decisão de segurança.</strong> Um <c>302</c> é o
/// jeito mais simples de burlar allowlist: o host autorizado responde e manda o cliente para outro
/// lugar. O cliente HTTP desta escada tem <c>AllowAutoRedirect = false</c>, e um redirecionamento
/// conta como documento não encontrado — não como novo alvo a visitar.
/// </para>
/// </remarks>
internal static class SafeUrlPolicy
{
    /// <summary>
    /// Confere se todos os endereços do host são públicos e roteáveis.
    /// </summary>
    /// <remarks>
    /// <strong>Reprova se qualquer um deles for interno</strong>, não se todos forem. Um host que
    /// resolve para um endereço público e um privado é exatamente a forma que um ataque de
    /// rebinding assume; aceitar o público seria aceitar a chance de o cliente escolher o outro.
    /// </remarks>
    public static async Task<bool> IsPubliclyRoutableAsync(string host, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        // Endereço literal não passa por DNS, e é a forma mais direta de tentar alcançar a rede
        // interna — a conferência é a mesma.
        if (IPAddress.TryParse(host, out var literal))
            return IsPublic(literal);

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);

            return addresses.Length > 0 && Array.TrueForAll(addresses, IsPublic);
        }
        catch (SocketException)
        {
            // Host que não resolve não é alcançável; recusar é o mesmo desfecho de não achar.
            return false;
        }
    }

    private static bool IsPublic(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return false;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return IsPublicV6(address);

        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        var octets = address.GetAddressBytes();

        return octets[0] switch
        {
            0 => false,                                   // "este host", nesta rede
            10 => false,                                  // RFC 1918
            127 => false,                                 // loopback
            169 when octets[1] == 254 => false,           // link-local — inclui o endereço de metadados de nuvem
            172 when octets[1] is >= 16 and <= 31 => false, // RFC 1918
            192 when octets[1] == 168 => false,           // RFC 1918
            100 when octets[1] is >= 64 and <= 127 => false, // CGNAT (RFC 6598)
            >= 224 => false,                              // multicast e reservado
            _ => true,
        };
    }

    private static bool IsPublicV6(IPAddress address)
    {
        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
            return false;

        // fc00::/7 — endereço local único, o equivalente v6 da faixa privada.
        var bytes = address.GetAddressBytes();
        if ((bytes[0] & 0xFE) == 0xFC)
            return false;

        // v4 mapeado em v6 contorna a checagem acima se não for desembrulhado.
        return !address.IsIPv4MappedToIPv6 || IsPublic(address.MapToIPv4());
    }
}
