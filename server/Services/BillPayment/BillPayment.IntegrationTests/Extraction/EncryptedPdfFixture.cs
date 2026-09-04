namespace BillPayment.IntegrationTests.Extraction;

/// <summary>
/// Um PDF cifrado de verdade, versionado como base64.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque nenhuma biblioteca do BC escreve PDF cifrado.</strong> O PdfPig lê a
/// cifra e não a produz, então o caminho da senha derivada só era exercitado pelos dois casos em
/// que <c>UnlockAsync</c> responde "nada a fazer" — e a prova de que ele de fato abre um arquivo
/// trancado dependia de conferência manual contra o acervo real. Este arquivo paga essa dívida.
/// </para>
/// <para>
/// <strong>RC4 de 40 bits (<c>/V 1 /R 2</c>), que é o que os emissores usam</strong> — a mesma
/// forma encontrada nos três boletos cifrados do acervo. A senha é <see cref="Password"/>, os
/// cinco primeiros dígitos de <see cref="TenantCnpj"/>: o formato que a medição de 2026-08-11
/// mostrou dominante, e o que faz a candidata <c>cnpj_first_5_primary</c> ser a que abre.
/// </para>
/// <para>
/// O conteúdo é uma página em branco. O que está sob teste é a cifra, não a extração — e um
/// documento com boleto de verdade aqui misturaria as duas coisas num fixture só.
/// </para>
/// </remarks>
internal static class EncryptedPdfFixture
{
    /// <summary>O CNPJ do tenant que abre este documento. O mesmo do resto da suíte.</summary>
    public const string TenantCnpj = "11.222.333/0001-81";

    /// <summary>A senha do PDF: os cinco primeiros dígitos do CNPJ, sem pontuação.</summary>
    public const string Password = "11222";

    private const string Base64 =
        "JVBERi0xLjMKJeLjz9MKMSAwIG9iago8PAovUHJvZHVjZXIgPDdlYjM5NGNhMjk+Cj4+CmVuZG9iagoyIDAgb2JqCjw8Ci9UeXBl"
        + "IC9QYWdlcwovQ291bnQgMQovS2lkcyBbIDQgMCBSIF0KPj4KZW5kb2JqCjMgMCBvYmoKPDwKL1R5cGUgL0NhdGFsb2cKL1BhZ2Vz"
        + "IDIgMCBSCj4+CmVuZG9iago0IDAgb2JqCjw8Ci9UeXBlIC9QYWdlCi9SZXNvdXJjZXMgPDwKPj4KL01lZGlhQm94IFsgMC4wIDAu"
        + "MCAyMDAgMjAwIF0KL1BhcmVudCAyIDAgUgo+PgplbmRvYmoKNSAwIG9iago8PAovViAxCi9SIDIKL0xlbmd0aCA0MAovUCA0Mjk0"
        + "OTY3MjkyCi9GaWx0ZXIgL1N0YW5kYXJkCi9PIDxhNTJhYjU2MWZkZjY1ODM1ZWUwMTUyY2Q1N2IzMDA3MWYyMDI5OWRmMGNlYWUy"
        + "YWJjZjUwYzRmNDgzNTc3YTZmPgovVSA8MzljMjFlMzBhZjliOWE0MWYyM2YzY2Q3MDFkNDVjNjJjNjZkZDg3ZDhmZjNhZDEwMTYy"
        + "Mjg5YWYxMmJiYThmYz4KPj4KZW5kb2JqCnhyZWYKMCA2CjAwMDAwMDAwMDAgNjU1MzUgZiAKMDAwMDAwMDAxNSAwMDAwMCBuIAow"
        + "MDAwMDAwMDU5IDAwMDAwIG4gCjAwMDAwMDAxMTggMDAwMDAgbiAKMDAwMDAwMDE2NyAwMDAwMCBuIAowMDAwMDAwMjYxIDAwMDAw"
        + "IG4gCnRyYWlsZXIKPDwKL1NpemUgNgovUm9vdCAzIDAgUgovSW5mbyAxIDAgUgovSUQgWyA8MzUzOTYzMzIzMDYyNjI2MTY1NjMz"
        + "ODMyNjUzMTYyMzU2MzM2MzM2MzYxNjI2NTY2MzU2MTY2NjE2NTY2MzEzMT4gPDM1Mzk2MzMyMzA2MjYyNjE2NTYzMzgzMjY1MzE2"
        + "MjM1NjMzNjMzNjM2MTYyNjU2NjM1NjE2NjYxNjU2NjMxMzE+IF0KL0VuY3J5cHQgNSAwIFIKPj4Kc3RhcnR4cmVmCjQ3NQolJUVP"
        + "Rgo=";

    /// <summary>Os bytes do documento cifrado.</summary>
    public static byte[] Bytes() => Convert.FromBase64String(Base64);
}
