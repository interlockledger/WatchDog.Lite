// ******************************************************************************************************************************
//  
// Copyright (c) 2022 InterlockLedger Network
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

using InterlockLedger.WatchDog.Interfaces;
using InterlockLedger.WatchDog.Models;
using InterlockLedger.WatchDog.Settings;

namespace InterlockLedger.WatchDog;
public class WatchDogMiddleware
{
    public static string? UserName { get; set; }
    public static string? Password { get; set; }
    public static string? RequiredRole { get; set; }

    private readonly RequestDelegate _next;
    private readonly IBroadcastHelper _broadcastHelper;
    private readonly MiddlewareSettings _options;
    private readonly IDBHelper _dbHelper;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly IModelsFilter _filter;
    private readonly string[] _blacklist;
    private readonly bool _logExceptions;

    public WatchDogMiddleware(MiddlewareSettings options,
                              RequestDelegate next,
                              IBroadcastHelper broadcastHelper,
                              IDBHelper dbHelper,
                              RecyclableMemoryStreamManager recyclableMemoryStreamManager,
                              IModelsFilter modelsFilter) {
        _next = next.Required();
        _options = options.Required();
        _broadcastHelper = broadcastHelper.Required();
        _dbHelper = dbHelper.Required();
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager.Required();
        _filter = modelsFilter.Required();
        UserName = _options.WatchPageUsername;
        Password = _options.WatchPagePassword;
        RequiredRole = _options.RequiredRole;
        _blacklist = string.IsNullOrEmpty(_options.Blacklist) ? Array.Empty<string>() : _options.Blacklist.Replace(" ", string.Empty).Split(',');
        _logExceptions = _options.LogExceptions;
    }

    public async Task InvokeAsync(HttpContext context) {
        if (ShouldLog(context.Request.Path.ToString()))
            await Log(context).ConfigureAwait(false);
        else
            await _next.Invoke(context).ConfigureAwait(false);
    }

    private bool ShouldLog(string requestPath) =>
        !requestPath.Contains("WTCHDwatchpage", StringComparison.OrdinalIgnoreCase) &&
        !requestPath.Contains("watchdog", StringComparison.OrdinalIgnoreCase) &&
        !requestPath.Contains("WTCHDGstatics", StringComparison.OrdinalIgnoreCase) &&
        !requestPath.Contains("favicon", StringComparison.OrdinalIgnoreCase) &&
        !requestPath.Contains("wtchdlogger", StringComparison.OrdinalIgnoreCase) &&
        !IsBlackListed(requestPath);
    private bool IsBlackListed(string requestPath) =>
        _blacklist.Contains(requestPath.Remove(0, 1), StringComparer.OrdinalIgnoreCase);

    public async Task LogException(Exception ex, RequestModel requestModel) {
        Debug.WriteLine("The following exception is logged: " + ex.Message);
        var watchExceptionLog = _filter.FilterExceptionLog(BuildExceptionLogModel(ex, requestModel), requestModel);
        //Insert
        _ = _dbHelper.InsertWatchExceptionLog(watchExceptionLog);
        await _broadcastHelper.BroadcastExLog(watchExceptionLog).ConfigureAwait(false);
    }

    private async Task Log(HttpContext context) {
        var requestLog = await BuildRequestModel(context).ConfigureAwait(false);
        var responseLog = _filter.FilterResponse(await ChainAndBuildResponseModel(context, requestLog).ConfigureAwait(false), requestLog);
        requestLog = _filter.FilterRequest(requestLog);
        var timeSpent = responseLog.FinishTime.Subtract(requestLog.StartTime);
        WatchLog watchLog = new() {
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            ResponseStatus = responseLog.ResponseStatus,
            QueryString = requestLog.QueryString,
            Method = requestLog.Method,
            Path = requestLog.Path,
            Host = requestLog.Host,
            RequestContentType = requestLog.ContentType,
            RequestContentLength = requestLog.ContentLength,
            RequestBody = requestLog.RequestBody,
            RequestHeaders = requestLog.Headers,
            ResponseContentType = responseLog.ContentType,
            ResponseContentLength = responseLog.ContentLength,
            ResponseBody = responseLog.ResponseBody,
            ResponseHeaders = responseLog.Headers,
            TimeSpent = string.Format("{0:D1} hrs {1:D1} mins {2:D1} secs {3:D1} ms", timeSpent.Hours, timeSpent.Minutes, timeSpent.Seconds, timeSpent.Milliseconds),
            StartTime = requestLog.StartTime,
            EndTime = responseLog.FinishTime
        };
        _ = _dbHelper.InsertWatchLog(watchLog);
        await _broadcastHelper.BroadcastWatchLog(watchLog).ConfigureAwait(false);
    }

    public async Task<ResponseModel> ChainAndBuildResponseModel(HttpContext context, RequestModel requestLog) {
        var originalBodyStream = context.Response.Body;
        try {
            using var newResponseBodyStream = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = newResponseBodyStream;
            try {
                await _next(context).ConfigureAwait(false);
            } catch (Exception ex) {
                if (_logExceptions)
                    await LogException(ex, requestLog).ConfigureAwait(false);
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(ex.GetType().FullName.WithDefault("Exception")).ConfigureAwait(false);
                await context.Response.WriteAsync(": ").ConfigureAwait(false);
                await context.Response.WriteAsync(ex.Message).ConfigureAwait(false);
                context.Response.StatusCode = 500;
            }
            _ = context.Response.Body.Seek(0, SeekOrigin.Begin);
            var model = await BuildResponseModel(context).ConfigureAwait(false);
            _ = context.Response.Body.Seek(0, SeekOrigin.Begin);
            await context.Response.Body.CopyToAsync(originalBodyStream).ConfigureAwait(false);
            return model;
        } finally {
            context.Response.Body = originalBodyStream;
        }
    }

    private static ExceptionLogModel BuildExceptionLogModel(Exception ex, RequestModel requestModel) => new() {
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

    private async Task<RequestModel> BuildRequestModel(HttpContext context) {
        var startTime = DateTime.Now;
        var requestModel = new RequestModel() {
            Host = context.Request.Host.ToString(),
            Path = context.Request.Path.ToString(),
            Method = context.Request.Method.ToString(),
            QueryString = context.Request.QueryString.ToString(),
            StartTime = startTime,
            Headers = context.Request.Headers.JoinedBy(": "),
            ContentLength = context.Request.ContentLength,
            ContentType = context.Request.ContentType,
            RequestBody = await RenderRequestBody(context).ConfigureAwait(false)
        };
        return requestModel;
    }

    private async Task<string> RenderRequestBody(HttpContext context) {
        switch (context.Request.ContentLength) {
        case > 1: {
                context.Request.EnableBuffering();
                try {
                    using var requestStream = _recyclableMemoryStreamManager.GetStream();
                    await context.Request.Body.CopyToAsync(requestStream).ConfigureAwait(false);
                    return await requestStream.RenderBodyAsync(context.Request.ContentType, context.Request.ContentLength).ConfigureAwait(false);
                } finally {
                    context.Request.Body.Position = 0;
                }
            }
        default:
            return string.Empty;
        }
    }

    private async Task<ResponseModel> BuildResponseModel(HttpContext context) {
        using var newStream = _recyclableMemoryStreamManager.GetStream();
        await context.Response.Body.CopyToAsync(newStream).ConfigureAwait(false);
        var model = new ResponseModel {
            ResponseBody = await newStream.RenderBodyAsync(context.Response.ContentType, context.Response.ContentLength).ConfigureAwait(false),
            ResponseStatus = context.Response.StatusCode,
            ContentLength = context.Response.ContentLength,
            ContentType = context.Response.ContentType,
            FinishTime = DateTime.Now,
            Headers = context.Response.Headers.JoinedBy(": "),
        };
        return model;
    }
}
