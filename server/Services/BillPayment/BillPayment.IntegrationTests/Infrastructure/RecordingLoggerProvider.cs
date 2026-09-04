namespace BillPayment.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provider de log que guarda em memória tudo o que a aplicação escreve, para um teste poder
/// afirmar sobre o conteúdo do log — que é onde um segredo vaza sem quebrar nada.
/// </summary>
/// <remarks>
/// Guarda a mensagem <strong>já formatada</strong>, e não os parâmetros estruturados, porque é a
/// forma formatada que chega ao console e ao arquivo. É nela que a substituição de <c>{@Command}</c>
/// pelo <c>ToString()</c> do record aconteceria — e é exatamente esse <c>ToString()</c> que
/// imprimiria a credencial se a redação do <c>BaseController</c> falhasse.
/// </remarks>
public sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _entries = new();

    public IReadOnlyCollection<string> Entries => [.. _entries];

    public bool AnyContains(string fragment)
        => _entries.Any(e => e.Contains(fragment, StringComparison.Ordinal));

    public void Clear() => _entries.Clear();

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(_entries);

    public void Dispose() => _entries.Clear();

    private sealed class RecordingLogger(ConcurrentQueue<string> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            entries.Enqueue(formatter(state, exception));
        }
    }
}
