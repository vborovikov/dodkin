namespace Dodkin.Service;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Data;
using Delivery;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.EventLog;
using Relay.RequestModel;

record ServiceOptions
{
    public const string ServiceName = "Dodkin";

    public MessageEndpoint Endpoint { get; init; } = MessageEndpoint.FromName(ServiceName.ToLowerInvariant());
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan WaitPeriod { get; init; } = TimeSpan.FromDays(1);
    public int RetryCount { get; init; } = MessageRecord.MaxRetryCount;
    public string TableName { get; init; } = "job.Delivery";
#if DEBUG    
    public bool PurgeOnStart { get; init; } = false;
#endif    
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
            configurationBuilder.AddCommandLine(args);
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
            var connectionString = hostContext.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found or empty.");
            }

            services.AddSingleton(_ => SqlClientFactory.Instance.CreateDataSource(connectionString));
            services.AddSingleton<IMessageStore, MessageStore>();
            // mq
            services.AddSingleton(MessageQueueFactory.Instance);
            // svc
            services.AddSingleton<IRequestDispatcher, RequestHandler>();
            services.AddSingleton<Messenger>();
            services.AddHostedService<Worker>();
        })
        .UseWindowsService(options =>
        {
            options.ServiceName = ServiceOptions.ServiceName;
        })
        .Build();
}