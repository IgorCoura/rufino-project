namespace BillPayment.API.BackgroundServices;

/// <summary>Configuração da varredura que reconsulta boletos que ficaram sem consulta oficial.</summary>
/// <remarks>
/// <strong>Ligada por padrão, e é o que torna verdadeiro o aviso da verificação 3.</strong> Desde
/// 2026-09-10 consulta sem resposta leva o boleto a Extremo Perigo com a mensagem "revalide mais
/// tarde"; desligada, essa frase vira trabalho manual que ninguém faz e o boleto fica na alçada
/// máxima por um incidente que já terminou.
/// </remarks>
public sealed class BillRevalidationOptions
{
    public const string SectionName = "BillRevalidation";

    /// <summary>Quando <c>false</c>, o worker não é registrado.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Intervalo entre ciclos. Sem boleto na fila o ciclo é uma consulta indexada vazia.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Quantos boletos por ciclo. Serial — o teto é o provedor, não o código.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>Espera-base entre tentativas do MESMO boleto. Dobra a cada tentativa acumulada.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Teto da espera. Sem ele o backoff exponencial adia para nunca.</summary>
    /// <remarks>
    /// <strong>Doze horas não é o que um boleto espera</strong> — é onde a cadência para de
    /// crescer. Com a base de 5 minutos dobrando, o teto só é alcançado depois de oito falhas
    /// seguidas, cerca de um dia e meio de provedor fora; até lá as tentativas acontecem em 5 min,
    /// 15 min, 35 min, 1h15, 2h35, 5h15 e 10h35. Quem não quer esperar tem o botão Revalidar, e
    /// o boleto continua aprovável com a alçada máxima o tempo todo.
    /// </remarks>
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// Quantas indisponibilidades seguidas encerram o ciclo antes da hora.
    /// </summary>
    /// <remarks>
    /// <strong>Protege o resto do sistema, não a varredura.</strong> O cliente de consulta tem
    /// disjuntor por cliente nomeado: martelar um provedor fora do ar o abre e derruba junto as
    /// validações <em>interativas</em> de quem está na tela. Com o provedor fora, insistir no lote
    /// inteiro não ensina nada.
    /// </remarks>
    public int UnavailableStreakAbort { get; set; } = 3;

    /// <summary>Depois de quantos ciclos seguidos sem nenhum sucesso o log passa a gritar.</summary>
    /// <remarks>
    /// <para>
    /// Não há teto de tentativas — desistir aqui deixaria o boleto em Extremo Perigo para sempre
    /// por um incidente passageiro. O que existe é alerta: molde do
    /// <c>BlockedStreakAlertThreshold</c> da conciliação de pagamento.
    /// </para>
    /// <para>
    /// <strong>Conta ciclos que TENTARAM</strong>, não ciclos do relógio (ver
    /// <see cref="BlockedCycleStreak"/>): com o backoff crescendo, os ciclos que reivindicam
    /// alguma coisa ficam cada vez mais raros, e seis deles correspondem a cerca de duas horas e
    /// meia de provedor fora.
    /// </para>
    /// </remarks>
    public int BlockedStreakAlertThreshold { get; set; } = 6;
}
