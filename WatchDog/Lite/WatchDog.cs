using Microsoft.AspNetCore.Http;
using Microsoft.IO;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using WatchDog.Lite.Helpers;
using WatchDog.Lite.Interfaces;
using WatchDog.Lite.Models;

namespace WatchDog.Lite {
    public class WatchDog {

        private readonly RequestDelegate _next;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly IBroadcastHelper _broadcastHelper;
        private readonly WatchDogOptionsModel _options;
        private static WatchDogConfigModel _config;
        private readonly IDBHelper _dbHelper;

        public static string UserName => _config.UserName;
        public static string Password => _config.Password;

        public WatchDog(WatchDogOptionsModel options, RequestDelegate next, IBroadcastHelper broadcastHelper, IDBHelper dbHelper) {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            _broadcastHelper = broadcastHelper ?? throw new ArgumentNullException(nameof(broadcastHelper));
            _dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
            _config = new WatchDogConfigModel() {
                UserName = _options.WatchPageUsername,
                Password = _options.WatchPagePassword,
                Blacklist = string.IsNullOrEmpty(_options.Blacklist) ? Array.Empty<string>() : _options.Blacklist.Replace(" ", string.Empty).Split(','),
                LogExceptions = _options.LogExceptions
            };
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
            !_config.IsBlackListed(requestPath);

        public async Task LogException(Exception ex, RequestModel requestModel) {
            Debug.WriteLine("The following exception is logged: " + ex.Message);
            var watchExceptionLog = new WatchExceptionLog {
                EncounteredAt = DateTime.Now,
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                Source = ex.Source,
                TypeOf = ex.GetType().ToString(),
                Path = requestModel?.Path,
                Method = requestModel?.Method,
                QueryString = requestModel?.QueryString,
                RequestBody = requestModel?.RequestBody
            };

            //Insert
            _dbHelper.InsertWatchExceptionLog(watchExceptionLog);
            await _broadcastHelper.BroadcastExLog(watchExceptionLog);
        }

        private async Task Log(HttpContext context) {
            RequestModel requestLog = await LogRequest(context);
            ResponseModel responseLog = await LogResponse(context, requestLog);
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
            _dbHelper.InsertWatchLog(watchLog);
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
            return requestBodyDto;
        }

        private async Task<ResponseModel> LogResponse(HttpContext context, RequestModel requestLog) {
            var originalBodyStream = context.Response.Body;
            try {
                using var newResponseBodyStream = _recyclableMemoryStreamManager.GetStream();
                context.Response.Body = newResponseBodyStream;
                try {
                    await _next(context);
                } catch (Exception ex) {
                    if (_config.LogExceptions)
                        await LogException(ex, requestLog);
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(ex.GetType().FullName);
                    await context.Response.WriteAsync(": ");
                    await context.Response.WriteAsync(ex.Message);
                    await context.Response.WriteAsync("\n");
                    await context.Response.WriteAsync(ex.StackTrace);
                    context.Response.StatusCode = 500;
                }
                return await ProcessResponse(context, originalBodyStream);
            } finally {
                context.Response.Body = originalBodyStream;
            }

            static async Task<ResponseModel> ProcessResponse(HttpContext context, Stream originalBodyStream) {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                string responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                var model = new ResponseModel {
                    ResponseBody = responseBody,
                    ResponseStatus = context.Response.StatusCode,
                    FinishTime = DateTime.Now,
                    Headers = context.Response.Headers.ContentLength > 0 ? context.Response.Headers.Select(x => x.ToString()).Aggregate((a, b) => a + ": " + b) : string.Empty,
                };
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await context.Response.Body.CopyToAsync(originalBodyStream);
                return model;
            }
        }
    }
}
