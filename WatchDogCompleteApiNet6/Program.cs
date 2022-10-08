using WatchDog;

internal class Program {
    private static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        builder.Services.AddWatchDogServices(opt => {
            opt.IsAutoClear = true;
            opt.ClearTimeSchedule = WatchDog.Lite.Enums.WatchDogAutoClearScheduleEnum.Daily;
            opt.DatabaseFolder = null;
        });
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()) {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.UseWatchDog(conf => {
            conf.WatchPageUsername = "admin";
            conf.WatchPagePassword = "Qwerty@123";
            conf.LogExceptions = true;
        });

        app.Run();
    }
}