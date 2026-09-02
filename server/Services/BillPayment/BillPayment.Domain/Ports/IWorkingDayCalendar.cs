namespace BillPayment.Domain.Ports;

/// <summary>
/// Dias úteis bancários nacionais. Snapshot embutido, não consulta ao vivo — mesma doutrina do
/// <see cref="IBankDirectory"/>: esta tabela decide a data em que dinheiro sai, e buscá-la em
/// tempo de agendamento transformaria indisponibilidade de terceiro em pagamento parado.
/// </summary>
/// <remarks>
/// Síncrono e sem <c>CancellationToken</c> de propósito — não é I/O. Fim de semana e feriado
/// bancário nacional contam como não útil; feriado municipal fica de fora, como no provedor.
/// </remarks>
public interface IWorkingDayCalendar
{
    bool IsWorkingDay(DateOnly date);

    /// <summary>A própria data quando ela é útil; senão, o primeiro dia útil seguinte.</summary>
    DateOnly NextWorkingDayOnOrAfter(DateOnly date);
}
