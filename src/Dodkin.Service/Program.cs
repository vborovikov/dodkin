namespace Dodkin.Service;

using System.Text;
using Microsoft.Extensions.Logging.EventLog;

record ServiceOptions
{
    public const string ServiceName = "Dodkin";
}

public static class Program
{
    public static void Main(string[] args)
    {
        if (Environment.UserInteractive)
        {
            Console.InputEncoding = Encoding.Default;
            Console.OutputEncoding = Encoding.Default;
        }

        var host = CreateHost(args);
        host.Run();
    }

    private static IHost CreateHost(string[] args) => Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostContext, configurationBuilder) =>
        {
#if DEBUG
            configurationBuilder.AddUserSecrets("DodkinService");
#endif
            configurationBuilder.AddEnvironmentVariables();
        })
        .ConfigureServices((hostContext, services) =>
        {
            services.Configure<ServiceOptions>(hostContext.Configuration.GetSection(ServiceOptions.ServiceName));
            services.Configure<EventLogSettings>(settings =>
            {
                // AddEventLog() is called in UseWindowsService()

                // The event log source must exist in the log or the app must have permissions to create it.
                // PowerShell command to create the source: New-EventLog -Source "Dodkin" -LogName "Application"
                settings.SourceName = ServiceOptions.ServiceName;
                settings.LogName = "Application";
            });
            services.AddHostedService<Worker>();
        })
        .UseWindowsService(options =>
        {
            options.ServiceName = ServiceOptions.ServiceName;
        })
        .Build();
}