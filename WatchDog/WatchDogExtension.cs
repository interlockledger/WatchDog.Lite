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

using InterlockLedger.WatchDog.Helpers;
using InterlockLedger.WatchDog.Hubs;
using InterlockLedger.WatchDog.Interfaces;
using InterlockLedger.WatchDog.Models;
using InterlockLedger.WatchDog.Services;
using InterlockLedger.WatchDog.Settings;

using Microsoft.AspNetCore.Routing;

#pragma warning disable IDE0058 // Expression value is never used

namespace InterlockLedger.WatchDog;

internal class UseWatchDog { }

public static class Extensions
{

    internal static readonly IFileProvider Provider =
        new EmbeddedFileProvider(typeof(Extensions).GetTypeInfo().Assembly, "InterlockLedger.WatchDog");

    public static TSettings ConfigureWith<TSettings>(Action<TSettings>? configureSettings, TSettings? settings = null) where TSettings : class, new() {
        settings ??= new();
        configureSettings?.Invoke(settings);
        return settings;
    }

    public static IServiceCollection AddWatchDogServices(this IServiceCollection services, [Optional] Action<ServicesSettings> configureOptions) {
        var settings = ConfigureWith(configureOptions);
        services.AddSignalR();
        services.AddMvcCore(x => { x.EnableEndpointRouting = false; })
                .AddApplicationPart(typeof(Extensions).Assembly);
        services.AddSingleton(settings)
                .AddSingleton<IDBHelper>(new LiteDBHelper(settings.DatabaseFolder))
                .AddSingleton<IBroadcastHelper, BroadcastHelper>();

        if (settings.UseAutoClear)
            services.AddHostedService(sp => BuildAutoLogClearerBackgroundService(sp));

        return services;

        static AutoLogClearerBackgroundService BuildAutoLogClearerBackgroundService(IServiceProvider sp) =>
           new(sp.GetRequiredService<ILogger<AutoLogClearerBackgroundService>>(),
               sp.GetRequiredService<IDBHelper>(),
               sp.GetRequiredService<ServicesSettings>().ClearTimeSchedule);
    }
    public static IApplicationBuilder UseWatchDog(this IApplicationBuilder app, Action<MiddlewareSettings> configureOptions) =>
        UseWatchDog(app, app.ApplicationServices, ConfigureWith(configureOptions));

    private static IApplicationBuilder UseWatchDog(IApplicationBuilder app, IServiceProvider sp, MiddlewareSettings settings) {
        settings.Validate();
        var dbHelper = sp.GetRequiredService<IDBHelper>();
        var servicesSettings = sp.GetRequiredService<ServicesSettings>();
        var logger = sp.GetRequiredService<ILogger<UseWatchDog>>();
        ServiceProviderFactory.BroadcastHelper = sp.GetRequiredService<IBroadcastHelper>();
        ServiceProviderFactory.DBHelper = dbHelper;
        logger.LogInformation("Logging database at '{folder}'", dbHelper.Folder.FullName);
        if (settings.LogExceptions)
            logger.LogInformation("Logging exceptions");
        if (settings.RequiredRole.IsBlank())
            logger.LogInformation("LogViewer will ask for credentials");
        else
            logger.LogInformation("LogViewer will require authenticated user to have role '{role}' or ask for credentials", settings.RequiredRole);
        if (servicesSettings.UseAutoClear)
            logger.LogInformation("Auto Log Clearing is enabled, with schedule: '{schedule}'", servicesSettings.ClearTimeSchedule);
        return
            app.UseRouting()
               .UseMiddleware<WatchDogMiddleware>(settings)
               .UseStaticFiles(new StaticFileOptions() {
                   FileProvider = new EmbeddedFileProvider(typeof(Extensions).GetTypeInfo().Assembly, "InterlockLedger.WatchDog.WatchPage"),
                   RequestPath = new PathString("/WTCHDGstatics")
               })
               .UseAuthorization();
    }

    public static void MapWatchDog(this IEndpointRouteBuilder endpoints) {
        endpoints.MapHub<LoggerHub>("/wtchdlogger");
        endpoints.MapControllerRoute(name: "WTCHDwatchpage",
                                     pattern: "WTCHDwatchpage/{action}",
                                     defaults: new { controller = "WatchPage", action = "Index" });
        endpoints.MapGet("/watchdog", context => SendWatchDogIndexPage(context));

        static async Task SendWatchDogIndexPage(HttpContext context) {
            var file = Provider.GetFileInfo("WatchPage.index.html");
            if (file.Exists) {
                context.Response.ContentType = "text/html";
                await context.Response.SendFileAsync(file).ConfigureAwait(false);
            } else {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Failed to load watchdog index page").ConfigureAwait(false);
            }
        }
    }
}
