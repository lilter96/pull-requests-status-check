using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PullRequestStatusNotifier.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace PullRequestStatusNotifier
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override(
                    "Microsoft", 
                    LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    @"/PullRequestStatusNotifierLogs/logs.txt",
                    rollingInterval: RollingInterval.Day, 
                    retainedFileCountLimit: null)
                .CreateLogger();

            try
            {
                Log.Information("Starting host...");
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly!");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "Pull Request Notifier";
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(context.Configuration);
                    services.AddSingleton<ISlackService, SlackService>();
                    services.AddSingleton<IStashService, StashService>();
                    services.AddHostedService<StalePullRequestNotificationService>();
                })
                .UseSerilog();
    }
}