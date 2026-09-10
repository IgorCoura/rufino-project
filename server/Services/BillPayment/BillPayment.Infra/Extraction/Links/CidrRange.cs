namespace BillPayment.Infra.Extraction.Links;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// Uma faixa de endereços em notação CIDR, já pronta para responder "este IP está aqui dentro?".
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe para a lista configurável, não para as faixas reservadas.</strong> As
/// reservadas são fixas e vivem em <see cref="SafeUrlPolicy"/> como código, porque não mudam e
/// porque uma delas esquecida é um buraco de SSRF — não é assunto de configuração. Esta struct
/// serve ao que só a instalação conhece: os endereços públicos da própria VPS, que o código não
/// tem como adivinhar.
/// </para>
/// <para>
/// <strong>A comparação é por bytes, com máscara.</strong> Converter para inteiro só funcionaria
/// em IPv4; o mesmo código precisa valer para <c>2001:db8::/32</c>, e endereço IPv6 não cabe em
/// <c>long</c>.
/// </para>
/// </remarks>
internal readonly struct CidrRange
{
    private readonly byte[] _network;
    private readonly int _prefixLength;
    private readonly AddressFamily _family;

    private CidrRange(byte[] network, int prefixLength, AddressFamily family)
    {
        _network = network;
        _prefixLength = prefixLength;
        _family = family;
    }

    /// <summary>
    /// Interpreta <c>10.0.0.0/8</c> ou <c>fd00::/8</c>. Endereço solto vira faixa de um IP só.
    /// </summary>
    /// <remarks>
    /// <strong>Aceitar o endereço sem barra não é conveniência.</strong> Quem configura a lista
    /// está pensando "o IP da minha VPS", não "a faixa /32 da minha VPS"; exigir a barra produz
    /// erro de digitação numa lista cujo erro de digitação é uma porta aberta.
    /// </remarks>
    public static bool TryParse(string? value, out CidrRange range)
    {
        range = default;

        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
            return false;

        var slash = text.IndexOf('/', StringComparison.Ordinal);
        var addressPart = slash < 0 ? text : text[..slash];

        if (!IPAddress.TryParse(addressPart, out var address))
            return false;

        var bytes = address.GetAddressBytes();
        var maxPrefix = bytes.Length * 8;

        int prefix;
        if (slash < 0)
        {
            prefix = maxPrefix;
        }
        else if (!int.TryParse(text[(slash + 1)..], out prefix) || prefix < 0 || prefix > maxPrefix)
        {
            return false;
        }

        // Zerar os bits fora do prefixo deixa a comparação barata e torna 10.1.2.3/8 equivalente
        // a 10.0.0.0/8 — que é o que quem escreveu quis dizer.
        MaskInPlace(bytes, prefix);

        range = new CidrRange(bytes, prefix, address.AddressFamily);
        return true;
    }

    public bool Contains(IPAddress address)
    {
        if (_network is null || address.AddressFamily != _family)
            return false;

        var candidate = address.GetAddressBytes();
        if (candidate.Length != _network.Length)
            return false;

        MaskInPlace(candidate, _prefixLength);

        return candidate.AsSpan().SequenceEqual(_network);
    }

    private static void MaskInPlace(byte[] bytes, int prefixLength)
    {
        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        if (remainingBits > 0 && fullBytes < bytes.Length)
        {
            bytes[fullBytes] &= (byte)(0xFF << (8 - remainingBits));
            fullBytes++;
        }

        for (var i = fullBytes; i < bytes.Length; i++)
            bytes[i] = 0;
    }
}
