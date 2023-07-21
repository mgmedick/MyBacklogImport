using System;
using Lamar;
using Serilog;
using Microsoft.Extensions.Hosting;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace GameStatsAppImport
{
    public class Program
    {
        static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            Serilog.Debugging.SelfLog.Enable(Console.WriteLine);
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                var processor = services.GetRequiredService<Processor>();
                processor.Run();
                Log.CloseAndFlush();
            }
        }

        private static IConfigurationRoot _config = null;
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseLamar()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddCommandLine(args);
                    _config = config.Build();
                })
                .UseSerilog((hostingContext, loggerConfiguration) =>
                {
                    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
                })
                .ConfigureContainer<Lamar.ServiceRegistry>((context, services) =>
                {
                    services.AddScoped<Processor>();
                    services.AddMemoryCache();
                    services.Scan(s =>
                    {
                        s.TheCallingAssembly();
                        s.Assembly("GameStatsAppImport.Repository");
                        s.Assembly("GameStatsAppImport.Service");
                        s.Assembly("GameStatsAppImport.Interfaces");
                        s.WithDefaultConventions();
                        s.SingleImplementationsOfInterface();
                    });
                })
                .UseConsoleLifetime();
    }
}
