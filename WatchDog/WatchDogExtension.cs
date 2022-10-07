using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using WatchDog.Lite;
using WatchDog.Lite.Exceptions;
using WatchDog.Lite.Helpers;
using WatchDog.Lite.Hubs;
using WatchDog.Lite.Interfaces;
using WatchDog.Lite.Models;
using WatchDog.Lite.Services;

namespace WatchDog {
    public static class WatchDogExtension {
        public static readonly IFileProvider Provider = new EmbeddedFileProvider(
            typeof(WatchDogExtension).GetTypeInfo().Assembly,
            "WatchDog"
        );

        public static IServiceCollection AddWatchDogServices(this IServiceCollection services, [Optional] Action<WatchDogSettings> configureOptions) {
            var options = new WatchDogSettings();
            configureOptions?.Invoke(options);

            AutoClearModel.IsAutoClear = options.IsAutoClear;
            AutoClearModel.ClearTimeSchedule = options.ClearTimeSchedule;
            services.AddSignalR();
            services.AddMvcCore(x => {
                x.EnableEndpointRouting = false;
            }).AddApplicationPart(typeof(WatchDogExtension).Assembly);


            services.AddSingleton<IBroadcastHelper, BroadcastHelper>();
            services.AddTransient<ILoggerService, LoggerService>();

            if (AutoClearModel.IsAutoClear)
                services.AddHostedService<AutoLogClearerBackgroundService>();

            return services;
        }

        public static IApplicationBuilder UseWatchDogExceptionLogger(this IApplicationBuilder builder) =>
            builder.UseMiddleware<WatchDogExceptionLogger>();

        public static IApplicationBuilder UseWatchDog(this IApplicationBuilder app, Action<WatchDogOptionsModel> configureOptions) {
            ServiceProviderFactory.BroadcastHelper = app.ApplicationServices.GetService<IBroadcastHelper>();
            var options = new WatchDogOptionsModel();
            configureOptions(options);
            if (string.IsNullOrEmpty(options.WatchPageUsername)) {
                throw new WatchDogAuthenticationException("Parameter Username is required on .UseWatchDog()");
            } else if (string.IsNullOrEmpty(options.WatchPagePassword)) {
                throw new WatchDogAuthenticationException("Parameter Password is required on .UseWatchDog()");
            }
            app.UseRouting();
            app.UseMiddleware<Lite.WatchDog>(options);
            app.UseStaticFiles(new StaticFileOptions() {
                FileProvider = new EmbeddedFileProvider(
                    typeof(WatchDogExtension).GetTypeInfo().Assembly,
                  "WatchDog.Lite.WatchPage"),

                RequestPath = new PathString("/WTCHDGstatics")
            });

            app.Build();

            app.UseAuthorization();

            return app.UseEndpoints(endpoints => {
                endpoints.MapHub<LoggerHub>("/wtchdlogger");
                endpoints.MapControllerRoute(
                    name: "WTCHDwatchpage",
                    pattern: "WTCHDwatchpage/{action}",
                    defaults: new { controller = "WatchPage", action = "Index" });
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapGet("watchdog", async context => {
                    await GetWatchDogFile(context);
                });
            });

        }

        private static async Task GetWatchDogFile(HttpContext context) {
            context.Response.ContentType = "text/html";
            IFileInfo file = WatchDogExtension.GetFile();
            await context.Response.SendFileAsync(file);
        }

        public static IFileInfo GetFile() {
            return Provider.GetFileInfo("Lite.WatchPage.index.html");

        }

        public static string GetFolder() {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }
    }
}
