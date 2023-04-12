using EmailArchival;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder();

builder.ConfigureLogging(cfg =>
{
    cfg.AddSimpleConsole(opt =>
    {
        opt.SingleLine = true;
        opt.UseUtcTimestamp = true;
        opt.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
        opt.TimestampFormat = "yyyy-MM-dd hh:mm:ss ";
        opt.IncludeScopes = false;
    });
});

builder.ConfigureServices(cfg =>
{
    cfg.AddHostedService<EmailChecker>();
});

var app = builder.Build();

await app.StartAsync();

