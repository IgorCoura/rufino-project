using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using PollyContext = Polly.Context;

namespace PeopleManagement.Infra.Policies
{
    /// <summary>
    /// Fábrica de políticas de resiliência HTTP usando Polly
    /// </summary>
    public static class HttpPolicyFactory
    {
        /// <summary>
        /// Política de retry com exponential backoff para erros transientes
        /// </summary>
        /// <param name="retryCount">Número de tentativas (padrão: 6)</param>
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount = 6)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        var logger = context.GetLogger();
                        logger?.LogWarning(
                            "Retry attempt {RetryAttempt} of {MaxRetries}. Waiting {Delay}s before retrying. Reason: {Reason}",
                            retryAttempt,
                            retryCount,
                            timespan.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
                    });
        }

        /// <summary>
        /// Política de retry mais agressiva para operações críticas (webhooks, etc)
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetAggressiveRetryPolicy(int retryCount = 16)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        var logger = context.GetLogger();
                        logger?.LogWarning(
                            "Aggressive retry {RetryAttempt} of {MaxRetries}. Waiting {Delay}s. Reason: {Reason}",
                            retryAttempt,
                            retryCount,
                            timespan.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
                    });
        }

        /// <summary>
        /// Circuit Breaker para evitar sobrecarga em caso de falhas contínuas
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, duration) =>
                    {
                        Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to continuous failures.");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("Circuit breaker closed. Normal requests resumed.");
                    },
                    onHalfOpen: () =>
                    {
                        Console.WriteLine("Circuit breaker in half-open state. Testing if service has recovered...");
                    });
        }

        /// <summary>
        /// Política de timeout para evitar requisições infinitas
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(int timeoutSeconds = 30)
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(timeoutSeconds),
                TimeoutStrategy.Optimistic);
        }

        /// <summary>
        /// Política combinada: Timeout + Retry + Circuit Breaker
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(
            int retryCount = 6,
            int timeoutSeconds = 30)
        {
            return Policy.WrapAsync(
                GetCircuitBreakerPolicy(),
                GetRetryPolicy(retryCount),
                GetTimeoutPolicy(timeoutSeconds));
        }

        /// <summary>
        /// Extensão para obter o logger do contexto do Polly
        /// </summary>
        private static ILogger? GetLogger(this PollyContext context)
        {
            if (context.TryGetValue("Logger", out var logger))
            {
                return logger as ILogger;
            }
            return null;
        }
    }
}
