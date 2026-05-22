namespace AccountsPayable.UnitTests.Payables.ValueObjects;

using System.Text;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// EMV BR Code payload — valida prefixo, comprimento e CRC16-CCITT-FALSE.
/// Helper local <see cref="WithCrc"/> computa o CRC e concatena no payload para facilitar
/// os casos positivos sem hardcoded strings frágeis.
/// </summary>
public class EmvPayloadTests
{
    // Payload válido mínimo: prefixo + algum conteúdo + CRC computado dinamicamente.
    private const string SAMPLE_PIX_BODY =
        "00020126360014BR.GOV.BCB.PIX0114+5511999998888"
        + "5204000053039865802BR5913FULANO DE TAL6008BRASILIA62070503***6304";

    // CRC16-CCITT-FALSE do "payload sem CRC" + os 4 chars hex retornados = EMV válido completo.
    private static string WithCrc(string bodyWithoutCrc) => bodyWithoutCrc + ComputeCrc(bodyWithoutCrc);

    private static string ComputeCrc(string data)
    {
        ushort crc = 0xFFFF;
        var bytes = Encoding.ASCII.GetBytes(data);
        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0) crc = (ushort)((crc << 1) ^ 0x1021);
                else crc <<= 1;
            }
        }
        return crc.ToString("X4");
    }

    // String vazia ou só com espaços lança AP.EMV01.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmpty_ShouldThrow_EMV01(string value)
    {
        var ex = Assert.Throws<DomainException>(() => new EmvPayload(value));
        Assert.Equal("AP.EMV01", ex.Id);
    }

    // Payload abaixo do tamanho mínimo lança AP.EMV02.
    [Fact]
    public void Constructor_TooShort_ShouldThrow_EMV02()
    {
        var ex = Assert.Throws<DomainException>(() => new EmvPayload("000201"));
        Assert.Equal("AP.EMV02", ex.Id);
    }

    // Payload acima do tamanho máximo lança AP.EMV03.
    [Fact]
    public void Constructor_TooLong_ShouldThrow_EMV03()
    {
        var huge = "000201" + new string('A', EmvPayload.MAX_LENGTH);
        var ex = Assert.Throws<DomainException>(() => new EmvPayload(huge));
        Assert.Equal("AP.EMV03", ex.Id);
    }

    // Prefixo diferente de "000201" lança AP.EMV04 (Payload Format Indicator obrigatório).
    [Fact]
    public void Constructor_InvalidPrefix_ShouldThrow_EMV04()
    {
        // Use um payload que passa nos comprimentos mas começa com prefixo errado.
        var corpo = "99999999" + new string('0', 24);
        var ex = Assert.Throws<DomainException>(() => new EmvPayload(corpo + "ABCD"));
        Assert.Equal("AP.EMV04", ex.Id);
    }

    // CRC inválido lança AP.EMV05 (4 últimos chars não batem com CRC16 computado).
    [Fact]
    public void Constructor_InvalidCrc_ShouldThrow_EMV05()
    {
        var payload = SAMPLE_PIX_BODY + "FFFF"; // CRC propositalmente errado
        var ex = Assert.Throws<DomainException>(() => new EmvPayload(payload));
        Assert.Equal("AP.EMV05", ex.Id);
    }

    // CRC válido (computado dinamicamente) constrói o VO e preserva o Value.
    [Fact]
    public void Constructor_ValidCrc_ShouldBuildAndPreserveValue()
    {
        var payload = WithCrc(SAMPLE_PIX_BODY);
        var vo = new EmvPayload(payload);
        Assert.Equal(payload, vo.Value);
    }

    // Trim externo é aplicado antes da validação.
    [Fact]
    public void Constructor_WithWhitespace_ShouldTrimBeforeValidation()
    {
        var payload = WithCrc(SAMPLE_PIX_BODY);
        var vo = new EmvPayload("  " + payload + "  ");
        Assert.Equal(payload, vo.Value);
    }

    // Igualdade estrutural por Value.
    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        var payload = WithCrc(SAMPLE_PIX_BODY);
        Assert.Equal(new EmvPayload(payload), new EmvPayload(payload));
    }
}
