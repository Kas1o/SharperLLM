using SharperLLM.API;

namespace SharperLLM.Agents;

public interface IToolExecutor
{
    Task<string?> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken);
}
