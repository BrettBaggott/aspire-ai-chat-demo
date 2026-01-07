using System.Runtime.CompilerServices;

public class KipperbitRunner : IKipperbitRunner
{
    public async IAsyncEnumerable<string> RunAsync(
        string prompt,
        RunnerContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var prefix = $"[kipperbit-ui stub | mode={context.Mode}] ";
        var words = (prefix + prompt).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return word + " ";
            await Task.Yield();
        }
    }
}
