using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmailArchival;
internal class MailService(IOptions<MailOptions> mailOptions, IImapClient imapClient, ILogger<MailService> logger) : IMailService
{
    private readonly MailOptions _mailOptions = mailOptions.Value;
    private readonly IImapClient _imapClient = imapClient;
    private readonly ILogger<MailService> _logger = logger;

    public async Task<IList<UniqueId>> GetMessagesFromInboxAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        await ConnectToImap(cancellationToken);
        _logger.LogDebug("Opened IMAP Server...");

        var query = SearchQuery.DeliveredBefore(beforeDate);

        await _imapClient.Inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        var messages = await _imapClient.Inbox.SearchAsync(query, cancellationToken);

        _logger.LogInformation("Deleting {NumberOfMessages} Messages", messages.Count);

        await _imapClient.DisconnectAsync(true, cancellationToken);
        _logger.LogDebug("Closed IMAP Server...");

        return messages;
    }
    public async Task<IList<UniqueId>> MoveEmailsToTrashAsync(IList<UniqueId> messages, CancellationToken cancellationToken = default)
    {
        await ConnectToImap(cancellationToken);

        var trash = _imapClient.GetFolder(SpecialFolder.Trash);

        await _imapClient.Inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
        await _imapClient.Inbox.MoveToAsync(messages, trash, cancellationToken);
        await _imapClient.DisconnectAsync(true, cancellationToken);

        return messages;
    }
    private async Task ConnectToImap(CancellationToken cancellationToken)
    {
        await _imapClient.ConnectAsync(_mailOptions.ServerName, _mailOptions.Port, _mailOptions.UseSSL, cancellationToken);
        await _imapClient.AuthenticateAsync(_mailOptions.Username, _mailOptions.Password, cancellationToken);
    }

}
