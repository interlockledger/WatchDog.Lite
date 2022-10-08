// ******************************************************************************************************************************
//  
// Copyright (c) 2018-2022 InterlockLedger Network
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met
//
// * Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// * Neither the name of the copyright holder nor the names of its
//   contributors may be used to endorse or promote products derived from
//   this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES, LOSS OF USE, DATA, OR PROFITS, OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
// ******************************************************************************************************************************

using InterlockLedger.WatchDog.Enums;
using InterlockLedger.WatchDog.Interfaces;

namespace InterlockLedger.WatchDog.Services;
internal class AutoLogClearerBackgroundService : BackgroundService
{
    private bool _isProcessing;
    private readonly ILogger<AutoLogClearerBackgroundService> _logger;
    private readonly IDBHelper _dBHelper;
    private readonly WatchDogAutoClearScheduleEnum _schedule;

    public AutoLogClearerBackgroundService(ILogger<AutoLogClearerBackgroundService> logger, IDBHelper dBHelper, WatchDogAutoClearScheduleEnum clearTimeSchedule) {
        _logger = logger;
        _dBHelper = dBHelper;
        _schedule = clearTimeSchedule;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            if (_isProcessing)
                return;
            _isProcessing = true;

            var minute = _schedule switch {
                WatchDogAutoClearScheduleEnum.Daily => TimeSpan.FromDays(1),
                WatchDogAutoClearScheduleEnum.Weekly => TimeSpan.FromDays(7),
                WatchDogAutoClearScheduleEnum.Monthly => TimeSpan.FromDays(30),
                WatchDogAutoClearScheduleEnum.Quarterly => TimeSpan.FromDays(90),
                _ => TimeSpan.FromDays(7),
            };
            var start = DateTime.UtcNow;
            while (true) {
                double remaining = (minute - (DateTime.UtcNow - start)).TotalMilliseconds;
                if (remaining <= 0)
                    break;
                if (remaining > short.MaxValue)
                    remaining = short.MaxValue;
                await Task.Delay(TimeSpan.FromMilliseconds(remaining), stoppingToken).ConfigureAwait(false);
            }
            try {
                _logger.LogInformation("Log Clearer Background service is starting");
                _logger.LogInformation("Log is clearing...");
                _ = _dBHelper.ClearAllLogs();
            } catch (Exception ex) {
                _logger.LogError("Log Clearer Background service error : {Message}", ex.Message);
            }
            _isProcessing = false;
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        _logger?.LogInformation("Log Clearer Background service is stopping");
        return Task.CompletedTask;
    }
}
