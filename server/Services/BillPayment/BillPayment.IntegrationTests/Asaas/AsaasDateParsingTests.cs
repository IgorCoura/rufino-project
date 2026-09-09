namespace BillPayment.IntegrationTests.Asaas;

using BillPayment.Infra.Asaas;

/// <summary>
/// A leitura de data e de instante do provedor.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Esta classe existe por causa de um pagamento que ficou horas sem ser registrado.</strong>
/// Em 2026-09-09 uma transação Pix <c>DONE</c> chegou com
/// <c>effectiveDate: "2026-09-09 11:54:16"</c>, e o <c>DateOnly.TryParse</c> que havia aqui
/// RECUSA esse formato — o campo virava <c>null</c>, "pago sem data de pagamento" batia na
/// invariante <c>BLP.PMO03</c> do agregado, e a conciliação daquela ordem estourava a cada ciclo,
/// para sempre.
/// </para>
/// <para>
/// O modo de falha é o que faz isto merecer teste próprio: a leitura mal-sucedida <strong>não é
/// exceção</strong>, é um <c>null</c> silencioso que só vira problema três camadas adiante.
/// </para>
/// </remarks>
public sealed class AsaasDateParsingTests
{
    // O formato de data-hora do provedor — o valor exato que quebrou em produção.
    [Theory]
    [InlineData("2026-09-09", 2026, 9, 9)]
    [InlineData("2026-09-09 11:54:16", 2026, 9, 9)]
    [InlineData("2026-09-09 11:54:16.123", 2026, 9, 9)]
    [InlineData("2026-09-09T11:54:16", 2026, 9, 9)]
    [InlineData("2026-09-09T11:54:16-03:00", 2026, 9, 9)]
    public void ReadDate_ShouldAcceptTheProvidersDateAndDateTimeShapes(
        string raw, int year, int month, int day)
    {
        var parsed = AsaasHttp.ReadDate(raw);

        Assert.Equal(new DateOnly(year, month, day), parsed);
    }

    // A data efetivada tarde da noite NÃO pode virar o dia seguinte. Se a leitura normalizasse
    // para UTC, "2026-09-09 22:30" (Brasília) viraria 2026-09-10 — e a trilha do pagamento
    // passaria a afirmar uma data que o provedor nunca disse.
    [Fact]
    public void ReadDate_WithALateNightTime_ShouldKeepTheProvidersDay()
    {
        Assert.Equal(new DateOnly(2026, 9, 9), AsaasHttp.ReadDate("2026-09-09 22:30:00"));
        Assert.Equal(new DateOnly(2026, 9, 9), AsaasHttp.ReadDate("2026-09-09 23:59:59"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nao é data")]
    public void ReadDate_WithNothingUsable_ShouldBeNull(string? raw)
        => Assert.Null(AsaasHttp.ReadDate(raw));

    // Instante SEM deslocamento é hora de Brasília, não UTC. Lido como UTC, um QR Pix que expira
    // às 23:59:59 seria considerado expirado três horas antes do real.
    [Fact]
    public void ReadTimestamp_WithoutAnOffset_ShouldAssumeTheProvidersTimeZone()
    {
        var parsed = AsaasHttp.ReadTimestamp("2029-06-20 23:59:59");

        Assert.NotNull(parsed);
        Assert.Equal(new DateTime(2029, 6, 20, 23, 59, 59), parsed!.Value.DateTime);

        // Brasília é UTC-3 (sem horário de verão desde 2019).
        Assert.Equal(TimeSpan.FromHours(-3), parsed.Value.Offset);
    }

    // Quando o texto DECLARA fuso, ele manda — nada é deslocado por cima.
    [Theory]
    [InlineData("2029-06-20T23:59:59Z", 0)]
    [InlineData("2029-06-20T23:59:59+00:00", 0)]
    [InlineData("2029-06-20T23:59:59-05:00", -5)]
    public void ReadTimestamp_WithAnExplicitOffset_ShouldRespectIt(string raw, int offsetHours)
    {
        var parsed = AsaasHttp.ReadTimestamp(raw);

        Assert.NotNull(parsed);
        Assert.Equal(TimeSpan.FromHours(offsetHours), parsed!.Value.Offset);
    }

    [Fact]
    public void ReadTimestamp_WithNothingUsable_ShouldBeNull()
        => Assert.Null(AsaasHttp.ReadTimestamp("nao é instante"));
}
