namespace Dodkin.Service;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.EventLog;
using Recorder;

record ServiceOptions
{
    public const string ServiceName = "Dodkin";

    public MessageEndpoint Endpoint { get; init; }
}

static class Program
{
    static Program()
    {
        DbTypes.Initialize();
    }

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

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
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

            // db
            services.AddSingleton<IDbFactory>(_ => new DbFactory(SqlClientFactory.Instance,
                hostContext.Configuration.GetConnectionString("Service")));
            services.AddSingleton<IMessageStore, MessageStore>();
            // mq
            services.AddSingleton<IMessageQueueFactory, MessageQueueFactory>();
            // svc
            services.AddHostedService<Worker>();
        })
        .UseWindowsService(options =>
        {
            options.ServiceName = ServiceOptions.ServiceName;
        })
        .Build();
}