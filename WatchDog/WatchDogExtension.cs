﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using WatchDog.src.Data;
using WatchDog.src.Exceptions;
using WatchDog.src.Helpers;
using WatchDog.src.Hubs;
using WatchDog.src.Interfaces;
using WatchDog.src.Models;
using WatchDog.src.Services;

namespace WatchDog
{
    public static class WatchDogExtension
    {
        public static readonly IFileProvider Provider = new EmbeddedFileProvider(
        typeof(WatchDogExtension).GetTypeInfo().Assembly,
        "WatchDog"
        );

        public static IServiceCollection AddWatchDogServices(this IServiceCollection services, [Optional] Action<WatchDogSettings> configureOptions)
        {
            var options = new WatchDogSettings();
            if (configureOptions != null)
                configureOptions(options);

            AutoClearModel.IsAutoClear = options.IsAutoClear;
            AutoClearModel.ClearTimeSchedule = options.ClearTimeSchedule;
            WatchDogExternalDbConfig.ConnectionString = options.SetExternalDbConnString;
            WatchDogSqlDriverOption.SqlDriverOption = options.SqlDriverOption;

            services.AddSignalR();
            services.AddMvcCore(x =>
            {
                x.EnableEndpointRouting = false;
            }).AddApplicationPart(typeof(WatchDogExtension).Assembly);


            services.AddTransient<IBroadcastHelper, BroadcastHelper>();
            services.AddTransient<ILoggerService, LoggerService>();

            if (!string.IsNullOrEmpty(WatchDogExternalDbConfig.ConnectionString))
            {
                ExternalDbContext.Migrate();
            }


            if (AutoClearModel.IsAutoClear)
                services.AddHostedService<AutoLogClearerBackgroundService>();
            return services;
        }

        public static IApplicationBuilder UseWatchDogExceptionLogger(this IApplicationBuilder builder)
        {

            return builder.UseMiddleware<src.WatchDogExceptionLogger>();
        }

        public static IApplicationBuilder UseWatchDog(this IApplicationBuilder app, Action<WatchDogOptionsModel> configureOptions)
        {
            var options = new WatchDogOptionsModel();
            configureOptions(options);
            if (string.IsNullOrEmpty(options.WatchPageUsername))
            {
                throw new WatchDogAuthenticationException("Parameter Username is required on .UseWatchDog()");
            }
            else if (string.IsNullOrEmpty(options.WatchPagePassword))
            {
                throw new WatchDogAuthenticationException("Parameter Password is required on .UseWatchDog()");
            }
                

            app.UseMiddleware<src.WatchDog>(options);


            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new EmbeddedFileProvider(
                    typeof(WatchDogExtension).GetTypeInfo().Assembly,
                  "WatchDog.src.WatchPage"),

                RequestPath = new PathString("/WTCHDGstatics")
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<LoggerHub>("/wtchdlogger");
                endpoints.MapControllerRoute(
                    name: "WTCHDwatchpage",
                    pattern: "WTCHDwatchpage/{action}",
                    defaults: new { controller = "WatchPage", action = "Index" });
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });


            app.Build();
            return app.UseRouter(router =>
            {
                router.MapGet("watchdog", async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.SendFileAsync(WatchDogExtension.GetFile());
                });

            });

        }


        public static IFileInfo GetFile()
        {
            return Provider.GetFileInfo("src.WatchPage.index.html");

        }

        public static string GetFolder()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }
    }
}
