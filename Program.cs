using EmailArchival;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Polly;
using MailKit.Net.Imap;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Options;

var builder = Host.CreateDefaultBuilder();

builder.ConfigureServices((hostContext, services) =>
{
    services.AddOptions<MailOptions>()
        .BindConfiguration(nameof(MailOptions));

    services.AddOptions<SeqOptions>()
        .BindConfiguration(nameof(SeqOptions));

    services.AddSerilog((provider, config) =>
    {
        var options = provider.GetRequiredService<IOptions<SeqOptions>>();

        config.MinimumLevel.Warning()
              .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
              .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
              .MinimumLevel.Override("EmailArchival", LogEventLevel.Information)
              .Enrich.FromLogContext()
#if !DEBUG
              .WriteTo.Seq(options.Value.Host, apiKey: options.Value.Key)
#endif
              .WriteTo.Console();
    });

    services.AddTransient<IMailService, MailService>();
    services.AddTransient<IImapClient, ImapClient>();

    services.AddResiliencePipeline("email-pipeline", (builder, context) =>
    {
        var predicate = new PredicateBuilder().Handle<Exception>();

        builder.InstanceName = "mainpipeline";

        builder.AddRetry(new()
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(5),
            UseJitter = true,
            ShouldHandle = predicate
        });
    });

    services.AddQuartz(cfg =>
    {
        var jobKey = JobKey.Create(nameof(EmailChecker));
        cfg.AddJob<EmailChecker>(jobKey);
        cfg.AddTrigger(cfg =>
                        cfg.ForJob(jobKey)
                            .StartNow()
                            .WithSimpleSchedule(builder =>
                                builder.WithIntervalInSeconds(3600).RepeatForever()));
    });

    services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);
});

var app = builder.Build();

await app.RunAsync();

