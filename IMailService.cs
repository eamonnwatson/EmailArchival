using MailKit;

namespace EmailArchival;
internal interface IMailService
{
    Task<IList<UniqueId>> GetMessagesFromInboxAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
    Task<IList<UniqueId>> MoveEmailsToTrashAsync(IList<UniqueId> messages, CancellationToken cancellationToken = default);
}