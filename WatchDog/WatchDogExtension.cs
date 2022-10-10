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
using InterlockLedger.WatchDog.Exceptions;
using InterlockLedger.WatchDog.Helpers;
using InterlockLedger.WatchDog.Hubs;
using InterlockLedger.WatchDog.Interfaces;
using InterlockLedger.WatchDog.Models;
using InterlockLedger.WatchDog.Services;
using InterlockLedger.WatchDog.Settings;

namespace InterlockLedger.WatchDog;
public static class WatchDogExtension
{
    public static readonly IFileProvider Provider =
        new EmbeddedFileProvider(typeof(WatchDogExtension).GetTypeInfo().Assembly, "InterlockLedger.WatchDog");

    public static IServiceCollection AddWatchDogServices(this IServiceCollection services, [Optional] Action<ServicesSettings> configureOptions) {
        var options = new ServicesSettings();
        configureOptions?.Invoke(options);

        _ = services.AddSignalR();
        _ = services.AddMvcCore(x => { x.EnableEndpointRouting = false; })
                    .AddApplicationPart(typeof(WatchDogExtension).Assembly);
        _ = services.AddSingleton<IDBHelper>(new LiteDBHelper(options.DatabaseFolder))
                    .AddSingleton<IBroadcastHelper, BroadcastHelper>();

        if (options.UseAutoClear)
            _ = services.AddHostedService(sp => sp.BuildAutoLogClearerBackgroundService(options.ClearTimeSchedule));

        return services;
    }

    private static AutoLogClearerBackgroundService BuildAutoLogClearerBackgroundService(
        this IServiceProvider sp,
        WatchDogAutoClearScheduleEnum clearTimeSchedule) => new(sp.GetRequiredService<ILogger<AutoLogClearerBackgroundService>>(),
                                                                sp.GetRequiredService<IDBHelper>(),
                                                                clearTimeSchedule);
    public static IApplicationBuilder UseWatchDog(this IApplicationBuilder app, Action<MiddlewareSettings> configureOptions) {
        ServiceProviderFactory.BroadcastHelper = app.ApplicationServices.GetService<IBroadcastHelper>();
        ServiceProviderFactory.DBHelper = app.ApplicationServices.GetService<IDBHelper>();
        var options = new MiddlewareSettings();
        configureOptions(options);
        if (string.IsNullOrEmpty(options.WatchPageUsername)) throw new WatchDogAuthenticationException("Parameter Username is required on .UseWatchDog()");
        else if (string.IsNullOrEmpty(options.WatchPagePassword)) throw new WatchDogAuthenticationException("Parameter Password is required on .UseWatchDog()");
        _ = app.UseRouting()
               .UseMiddleware<WatchDog>(options)
               .UseStaticFiles(new StaticFileOptions() {
                   FileProvider = new EmbeddedFileProvider(typeof(WatchDogExtension).GetTypeInfo().Assembly, "InterlockLedger.WatchDog.WatchPage"),
                   RequestPath = new PathString("/WTCHDGstatics")
               })
               .Build();
        return app
            .UseAuthorization()
            .UseEndpoints(endpoints => {
                _ = endpoints.MapHub<LoggerHub>("/wtchdlogger");
                _ = endpoints.MapControllerRoute(
                        name: "WTCHDwatchpage",
                        pattern: "WTCHDwatchpage/{action}",
                        defaults: new { controller = "WatchPage", action = "Index" });
                _ = endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");
                _ = endpoints.MapGet("watchdog", context => SendWatchDogIndexPage(context));
            });

    }

    private static async Task SendWatchDogIndexPage(HttpContext context) {
        var file = Provider.GetFileInfo("WatchPage.index.html");
        if (file.Exists) {
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync(file).ConfigureAwait(false);
        } else {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Failed to load index page").ConfigureAwait(false);
        }
    }
}
