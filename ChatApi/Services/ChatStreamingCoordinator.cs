using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

public class ChatStreamingCoordinator(
    IKipperbitRunner runner,
    IConversationStore store,
    ILogger<ChatStreamingCoordinator> logger,
    IConversationState conversationState,
    ICancellationManager cancellationManager,
    IConfiguration configuration)
{
    private readonly TimeSpan DefaultStreamItemTimeout = TimeSpan.FromMinutes(1);

    public async Task AddStreamingMessage(Guid conversationId, string text)
    {
        var promptId = Guid.CreateVersion7();
        store.AddMessage(conversationId, new ConversationChatMessage
        {
            Id = promptId,
            Role = ChatRole.User.Value,
            Text = text
        });

        var promptFragment = new ClientMessageFragment(promptId, ChatRole.User.Value, text, Guid.CreateVersion7(), IsFinal: true);
        await conversationState.PublishFragmentAsync(conversationId, promptFragment);

        _ = Task.Run(StreamReplyAsync);

        async Task StreamReplyAsync()
        {
            var assistantReplyId = Guid.CreateVersion7();
            logger.LogInformation("Adding streaming message for conversation {ConversationId} {MessageId}", conversationId, assistantReplyId);

            var token = cancellationManager.GetCancellationToken(assistantReplyId);
            var fragment = new ClientMessageFragment(assistantReplyId, ChatRole.Assistant.Value, "Generating reply...", Guid.CreateVersion7());
            await conversationState.PublishFragmentAsync(conversationId, fragment);

            var fullMessage = new List<string>();
            try
            {
                using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                tokenSource.CancelAfter(DefaultStreamItemTimeout);

                var context = new RunnerContext(
                    configuration["KIPPERBIT_SHARED_ROOT"] ?? string.Empty,
                    configuration["KIPPERBIT_REPOS_ROOT"] ?? string.Empty,
                    configuration["KIPPERBIT_MODE"] ?? "read-only");

                await foreach (var chunk in runner.RunAsync(text, context, tokenSource.Token).WithCancellation(tokenSource.Token))
                {
                    tokenSource.CancelAfter(DefaultStreamItemTimeout);

                    fullMessage.Add(chunk);
                    fragment = new ClientMessageFragment(assistantReplyId, ChatRole.Assistant.Value, chunk, Guid.CreateVersion7());
                    await conversationState.PublishFragmentAsync(conversationId, fragment);
                }

                var combined = string.Concat(fullMessage);
                if (!string.IsNullOrWhiteSpace(combined))
                {
                    store.AddMessage(conversationId, new ConversationChatMessage
                    {
                        Id = assistantReplyId,
                        Role = ChatRole.Assistant.Value,
                        Text = combined
                    });
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Streaming message cancelled for conversation {ConversationId} {MessageId}", conversationId, assistantReplyId);
            }
            catch (Exception ex)
            {
                fragment = new ClientMessageFragment(assistantReplyId, ChatRole.Assistant.Value, "Error streaming message", Guid.CreateVersion7());
                await conversationState.PublishFragmentAsync(conversationId, fragment);
                logger.LogError(ex, "Error streaming message for conversation {ConversationId} {MessageId}", conversationId, assistantReplyId);
            }
            finally
            {
                fragment = new ClientMessageFragment(assistantReplyId, ChatRole.Assistant.Value, "", Guid.CreateVersion7(), IsFinal: true);
                await conversationState.PublishFragmentAsync(conversationId, fragment);
                await conversationState.CompleteAsync(conversationId, assistantReplyId);
                await cancellationManager.CancelAsync(assistantReplyId);
            }
        }
    }

    public async IAsyncEnumerable<ClientMessageFragment> GetMessageStream(
        Guid conversationId,
        Guid? lastMessageId,
        Guid? lastDeliveredFragment,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting message stream for conversation {ConversationId}, {LastMessageId}", conversationId, lastMessageId);
        var stream = conversationState.Subscribe(conversationId, lastMessageId, cancellationToken);

        await foreach (var fragment in stream.WithCancellation(cancellationToken))
        {
            if (lastDeliveredFragment is null || fragment.FragmentId > lastDeliveredFragment)
            {
                lastDeliveredFragment = fragment.FragmentId;
            }
            else
            {
                continue;
            }

            yield return fragment;
        }
    }
}
