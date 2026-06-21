using SharperLLM.FunctionCalling;
using SharperLLM.Util;

namespace SharperLLM.Agents;

public interface IPromptContext
{
    PromptBuilder BuildPromptBuilder(IReadOnlyList<Tool> toolDefinitions);
    void AppendAssistantMessage(ChatMessage assistantMessage);
    void AppendToolResult(string toolCallId, string toolName, string toolResult);
}
