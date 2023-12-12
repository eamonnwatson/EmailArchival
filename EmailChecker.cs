using Polly;
using Polly.Registry;
using Quartz;

namespace EmailArchival;
[DisallowConcurrentExecution]
internal class EmailChecker(IMailService mailService, ResiliencePipelineProvider<string> resiliencePipeline) : IJob
{
    private readonly IMailService _mailService = mailService;
    private readonly ResiliencePipeline _pipeline = resiliencePipeline.GetPipeline("email-pipeline");

    public async Task Execute(IJobExecutionContext context)
    {
        await _pipeline.ExecuteAsync(async token =>
        {
            var messages = await _mailService.GetMessagesFromInboxAsync(DateTime.Today.AddDays(-90), token);
            await _mailService.MoveEmailsToTrashAsync(messages, token);
        }, 
        context.CancellationToken);

    }
}
