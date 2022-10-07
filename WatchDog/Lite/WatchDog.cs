using Microsoft.AspNetCore.Http;
using Microsoft.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using WatchDog.Lite.Helpers;
using WatchDog.Lite.Interfaces;
using WatchDog.Lite.Managers;
using WatchDog.Lite.Models;

namespace WatchDog.Lite {
    internal class WatchDog {
        public static RequestModel RequestLog;
        private readonly RequestDelegate _next;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly IBroadcastHelper _broadcastHelper;
        private readonly WatchDogOptionsModel _options;

        public WatchDog(WatchDogOptionsModel options, RequestDelegate next, IBroadcastHelper broadcastHelper) {
            _next = next;
            _options = options;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            _broadcastHelper = broadcastHelper;

            WatchDogConfigModel.UserName = _options.WatchPageUsername;
            WatchDogConfigModel.Password = _options.WatchPagePassword;
            WatchDogConfigModel.Blacklist = string.IsNullOrEmpty(_options.Blacklist) ? Array.Empty<string>() : _options.Blacklist.Replace(" ", string.Empty).Split(',');
        }

        public async Task InvokeAsync(HttpContext context) {
            if (ShouldLog(context.Request.Path.ToString())) {
                await Log(context);
            } else {
                await _next.Invoke(context);
            }
        }

        private static bool ShouldLog(string requestPath) =>
            !requestPath.Contains("WTCHDwatchpage", StringComparison.OrdinalIgnoreCase) &&
            !requestPath.Contains("watchdog", StringComparison.OrdinalIgnoreCase) &&
            !requestPath.Contains("WTCHDGstatics", StringComparison.OrdinalIgnoreCase) &&
            !requestPath.Contains("favicon", StringComparison.OrdinalIgnoreCase) &&
            !requestPath.Contains("wtchdlogger", StringComparison.OrdinalIgnoreCase) &&
            !WatchDogConfigModel.IsBlackListed(requestPath);

        private async Task Log(HttpContext context) {
            RequestModel requestLog = await LogRequest(context);
            ResponseModel responseLog = await LogResponse(context);
            var timeSpent = responseLog.FinishTime.Subtract(requestLog.StartTime);
            WatchLog watchLog = new() {
                IpAddress = context.Connection.RemoteIpAddress.ToString(),
                ResponseStatus = responseLog.ResponseStatus,
                QueryString = requestLog.QueryString,
                Method = requestLog.Method,
                Path = requestLog.Path,
                Host = requestLog.Host,
                RequestBody = requestLog.RequestBody,
                ResponseBody = responseLog.ResponseBody,
                TimeSpent = string.Format("{0:D1} hrs {1:D1} mins {2:D1} secs {3:D1} ms", timeSpent.Hours, timeSpent.Minutes, timeSpent.Seconds, timeSpent.Milliseconds),
                RequestHeaders = requestLog.Headers,
                ResponseHeaders = responseLog.Headers,
                StartTime = requestLog.StartTime,
                EndTime = responseLog.FinishTime
            };
            await DynamicDBManager.InsertWatchLog(watchLog);
            await _broadcastHelper.BroadcastWatchLog(watchLog);
        }

        private async Task<RequestModel> LogRequest(HttpContext context) {
            var startTime = DateTime.Now;
            List<string> requestHeaders = new();

            var requestBodyDto = new RequestModel() {
                RequestBody = string.Empty,
                Host = context.Request.Host.ToString(),
                Path = context.Request.Path.ToString(),
                Method = context.Request.Method.ToString(),
                QueryString = context.Request.QueryString.ToString(),
                StartTime = startTime,
                Headers = context.Request.Headers.Select(x => x.ToString()).Aggregate((a, b) => a + ": " + b),
            };


            if (context.Request.ContentLength > 1) {
                context.Request.EnableBuffering();
                await using var requestStream = _recyclableMemoryStreamManager.GetStream();
                await context.Request.Body.CopyToAsync(requestStream);
                requestBodyDto.RequestBody = GeneralHelper.ReadStreamInChunks(requestStream);
                context.Request.Body.Position = 0;
            }
            RequestLog = requestBodyDto;
            return requestBodyDto;
        }

        private async Task<ResponseModel> LogResponse(HttpContext context) {
            var responseBody = string.Empty;
            using var originalBodyStream = context.Response.Body;
            try {
                using var originalResponseBody = _recyclableMemoryStreamManager.GetStream();
                context.Response.Body = originalResponseBody;
                await _next(context);
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseBodyDto = new ResponseModel {
                    ResponseBody = responseBody,
                    ResponseStatus = context.Response.StatusCode,
                    FinishTime = DateTime.Now,
                    Headers = context.Response.Headers.ContentLength > 0 ? context.Response.Headers.Select(x => x.ToString()).Aggregate((a, b) => a + ": " + b) : string.Empty,
                };
                await originalResponseBody.CopyToAsync(originalBodyStream);
                return responseBodyDto;
            } finally {
                context.Response.Body = originalBodyStream;
            }
        }
    }
}
