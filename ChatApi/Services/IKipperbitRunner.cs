public interface IKipperbitRunner
{
    IAsyncEnumerable<string> RunAsync(
        string prompt,
        RunnerContext context,
        CancellationToken cancellationToken);
}

public record RunnerContext(string WorkspaceRoot, string ReposRoot, string Mode);
