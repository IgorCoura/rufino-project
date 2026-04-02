using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace PeopleManagement.Infra.Services
{
    public interface IBrowserProvider
    {
        Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken = default);
    }

    public sealed class BrowserProvider(ILogger<BrowserProvider> logger) : IBrowserProvider, IAsyncDisposable
    {
        private readonly ILogger<BrowserProvider> _logger = logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private IBrowser? _browser;

        private static readonly string[] ChromeArgs =
        [
            "--no-sandbox",
            "--disable-setuid-sandbox",
            "--disable-gpu",
            "--disable-dev-shm-usage",
            "--disable-extensions",
            "--disable-background-networking",
            "--disable-default-apps",
            "--disable-translate",
            "--no-first-run",
            "--font-render-hinting=none"
        ];

        public async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken = default)
        {
            if (_browser is { IsConnected: true })
                return _browser;

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_browser is { IsConnected: true })
                    return _browser;

                _logger.LogInformation("Initializing Puppeteer browser instance.");

                var executablePath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");

                if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
                {
                    _logger.LogWarning("No pre-installed Chrome found at PUPPETEER_EXECUTABLE_PATH. Downloading via BrowserFetcher.");
                    var browserFetcher = new BrowserFetcher();
                    var installedBrowser = await browserFetcher.DownloadAsync();
                    executablePath = installedBrowser.GetExecutablePath();
                }

                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = executablePath,
                    Args = ChromeArgs
                });

                _logger.LogInformation("Puppeteer browser initialized successfully.");
                return _browser;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser is not null)
            {
                await _browser.CloseAsync();
                _browser.Dispose();
            }
            _semaphore.Dispose();
        }
    }
}
