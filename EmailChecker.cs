using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmailArchival;
internal class EmailChecker : IHostedService
{
    private readonly ILogger<EmailChecker> logger;
    public int Frequency { get; private set; }
    public string Username { get; private set; }
    public string Password { get; private set; }
    public int Port { get; private set; }
    public string ServerName { get; private set; }
    public bool UseSSL { get; private set; }
    public int NumDays { get; private set; }
    public DateTime TheDate { get; private set; }

    public EmailChecker(IConfiguration config, ILogger<EmailChecker> logger)
	{
        Frequency = config.GetValue("EMAIL_FREQUENCY", 3600);
        Username = config.GetValue<string>("EMAIL_USERNAME") ?? string.Empty;
        Password = config.GetValue<string>("EMAIL_PASSWORD") ?? string.Empty;
        Port = config.GetValue("EMAIL_PORT", 993);
        ServerName = config.GetValue<string>("EMAIL_SERVER") ?? string.Empty;
        UseSSL = config.GetValue("EMAIL_USESSL", true);
        NumDays = config.GetValue("EMAIL_NUMDAYS", 60);
        TheDate = DateTimeOffset.Now.AddDays(NumDays * -1).Date;

        this.logger = logger;

        logger.LogInformation("Frequency of updates : {Frequency} ms", Frequency);
    }

    private async Task<IImapClient?> GetIMAPAsync(CancellationToken cancellationToken)
    {
        var imap = new ImapClient();
        await imap.ConnectAsync(ServerName, Port, UseSSL, cancellationToken);
        await imap.AuthenticateAsync(Username, Password, cancellationToken);

        if (!imap.IsAuthenticated)
            return null;

        return imap;
    }

    private async Task MoveEmailsToTrashAsync(IList<UniqueId> messages, CancellationToken cancellationToken)
    {
        using var imap = await GetIMAPAsync(cancellationToken) ?? throw new Exception("Not Authenticated");

        await imap.Inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
        await imap.Inbox.MoveToAsync(messages, await imap.GetFolderAsync("Trash", cancellationToken), cancellationToken);
        await imap.DisconnectAsync(true, cancellationToken);
    }

    private async Task<IList<UniqueId>> GetEmailsAsync(CancellationToken cancellationToken)
    {
        using var imap = await GetIMAPAsync(cancellationToken) ?? throw new Exception("Not Authenticated");

        IList<UniqueId> messages = new List<UniqueId>();

        var query = SearchQuery.DeliveredBefore(TheDate);

        await imap.Inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        messages = await imap.Inbox.SearchAsync(query, cancellationToken);

        await imap.DisconnectAsync(true, cancellationToken);

        return messages;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrEmpty(ServerName))
        {
            logger.LogCritical("Username and/or Password were not specified.  Ceasing Operation");
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var messages = await GetEmailsAsync(cancellationToken);
                logger.LogInformation("{NumEmails} Emails Found prior to {priordate:yyyy-MM-dd}", messages.Count, TheDate);
                await MoveEmailsToTrashAsync(messages, cancellationToken);
                logger.LogInformation("Emailed moved to trash");

                await Task.Delay(1000 * Frequency, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("Email Checking stopped");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "An error occured checking mail");
            }
        }        
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Email Checking stopped");
        return Task.CompletedTask;
    }
}
