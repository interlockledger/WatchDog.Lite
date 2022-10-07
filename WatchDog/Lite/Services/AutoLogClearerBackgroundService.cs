using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

using WatchDog.Lite.Enums;
using WatchDog.Lite.Interfaces;
using WatchDog.Lite.Models;

namespace WatchDog.Lite.Services {
    internal class AutoLogClearerBackgroundService : BackgroundService {
        private bool isProcessing;
        private readonly ILogger<AutoLogClearerBackgroundService> logger;
        private readonly IServiceProvider serviceProvider;

        public AutoLogClearerBackgroundService(ILogger<AutoLogClearerBackgroundService> logger, IServiceProvider serviceProvider) {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                if (isProcessing)
                    return;
                isProcessing = true;

                var schedule = AutoClearModel.ClearTimeSchedule;
                var minute = schedule switch {
                    WatchDogAutoClearScheduleEnum.Daily => TimeSpan.FromDays(1),
                    WatchDogAutoClearScheduleEnum.Weekly => TimeSpan.FromDays(7),
                    WatchDogAutoClearScheduleEnum.Monthly => TimeSpan.FromDays(30),
                    WatchDogAutoClearScheduleEnum.Quarterly => TimeSpan.FromDays(90),
                    _ => TimeSpan.FromDays(7),
                };
                var start = DateTime.UtcNow;
                while (true) {
                    var remaining = (minute - (DateTime.UtcNow - start)).TotalMilliseconds;
                    if (remaining <= 0)
                        break;
                    if (remaining > short.MaxValue)
                        remaining = short.MaxValue;
                    await Task.Delay(TimeSpan.FromMilliseconds(remaining), stoppingToken);
                }
                DoWork();
                isProcessing = false;
            }
        }

        private void DoWork() {
            try {
                using var scope = serviceProvider.CreateScope();
                var loggerService = scope.ServiceProvider.GetService<ILoggerService>();
                try {
                    logger.LogInformation("Log Clearer Background service is starting");
                    logger.LogInformation($"Log is clearing...");
                    loggerService.ClearWatchLogs();
                } catch (Exception ex) {
                    logger.LogError("{message}", ex.Message);
                }
            } catch (Exception ex) {
                logger.LogError("Log Clearer Background service error : {Message}", ex.Message);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken) {
            logger?.LogInformation("Log Clearer Background service is stopping");
            return Task.CompletedTask;
        }

    }
}
