namespace BillPayment.API.BackgroundServices;

/// <summary>
/// Configuração do agendador de purga do livro-caixa da captura.
/// </summary>
/// <remarks>
/// <strong>O worker vem ligado, e a purga não.</strong> São coisas diferentes: quem decide se
/// algum registro é apagado é a política de cada tenant, que nasce desligada. Um worker desligado
/// por padrão faria a política ligada não valer nada, e a pessoa concluiria que o prazo escolhido
/// na tela não funciona.
/// </remarks>
internal sealed class CaptureRetentionOptions
{
    public const string SectionName = "CaptureRetention";

    /// <summary>Registra o worker. Deixe <c>true</c> em UM deployment só ao escalar.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Ritmo da varredura. Diário como o das expectativas — o que ela observa é uma data-limite
    /// que muda de dia em dia, e rodar de minuto em minuto só gastaria transação para reencontrar
    /// o mesmo estado.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(12);

    /// <summary>Quantos registros por ciclo, por tenant. O resto sai no ciclo seguinte.</summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>Quantas políticas por ciclo.</summary>
    public int TenantBatchSize { get; set; } = 50;
}
