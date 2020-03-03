using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication.HostedServices
{
    internal class TimedHostedService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private DateTime _started;
        private Stopwatch _sw = new Stopwatch();
        private Timer _timer;

        public TimedHostedService(ILogger<TimedHostedService> logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is starting.");

            cancellationToken.Register(() => _logger.LogInformation("Timed background Service is stopping..."));

            _started = DateTime.UtcNow;
            _sw.Start();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    DoWork();

                    await Task.Delay(5000, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {

            }

            _logger.LogInformation("Timed Background Service has stopped.");

            //return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is aborting.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void DoWork()
        {
            _logger.LogInformation("Timed Background Service is working since {Started:yyyy-MM-dd HH:mm:ss} ({Elapsed:c}).", _started, _sw.Elapsed);
        }
    }
}