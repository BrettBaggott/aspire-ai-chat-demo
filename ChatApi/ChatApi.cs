public static class ChatExtensions
{
    public static void MapChatApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/chat");

        group.MapGet("/", (IConversationStore store) => store.GetAll());

        group.MapGet("/settings", (IConfiguration config) =>
        {
            var workspaceRoot = config["KIPPERBIT_SHARED_ROOT"] ?? string.Empty;
            var reposRoot = config["KIPPERBIT_REPOS_ROOT"] ?? string.Empty;
            var mode = config["KIPPERBIT_MODE"];
            if (string.IsNullOrWhiteSpace(mode))
            {
                mode = "read-only";
            }

            return Results.Ok(new RunnerDefaults(workspaceRoot, reposRoot, mode));
        });

        group.MapGet("/{id}", (Guid id, IConversationStore store) =>
        {
            var conversation = store.Get(id);

            if (conversation is null)
            {
                return Results.NotFound();
            }

            var clientMessages = from m in conversation.Messages
                                 select new ClientMessage(m.Id, m.Role, m.Text);

            return Results.Ok(clientMessages);
        });

        group.MapHub<ChatHub>("/stream", o => o.AllowStatefulReconnects = true);

        group.MapPost("/", (NewConversation newConversation, IConversationStore store) =>
        {
            if (string.IsNullOrWhiteSpace(newConversation.Name))
            {
                return Results.BadRequest();
            }

            var conversation = store.Create(newConversation.Name);

            return Results.Created($"/api/chats/{conversation.Id}", conversation);
        });

        group.MapPost("/{id}", async (Guid id, IConversationStore store, Prompt prompt, ChatStreamingCoordinator streaming) =>
        {
            if (store.Get(id) is null)
            {
                return Results.NotFound();
            }

            // Fire and forget
            await streaming.AddStreamingMessage(id, prompt.Text, prompt.Context);

            return Results.Ok();
        });

        group.MapPost("/{id}/cancel", async (Guid id, ICancellationManager cancellationManager) =>
        {
            await cancellationManager.CancelAsync(id);

            return Results.Ok();
        });

        group.MapDelete("/{id}", (Guid id, IConversationStore store) =>
            store.Delete(id) ? Results.Ok() : Results.NotFound());
    }
}

public record Prompt(string Text, RunnerContext? Context);

public record NewConversation(string Name);

public record ClientMessage(Guid Id, string Sender, string Text);

public record ClientMessageFragment(Guid Id, string Sender, string Text, Guid FragmentId, bool IsFinal = false);

public record StreamContext(Guid? LastMessageId, Guid? LastFragmentId);

public record RunnerDefaults(string WorkspaceRoot, string ReposRoot, string Mode);
