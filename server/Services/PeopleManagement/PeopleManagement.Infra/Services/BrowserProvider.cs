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
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();

                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = ["--no-sandbox", "--disable-setuid-sandbox"]
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
