using SharperLLM.FunctionCalling;
using SharperLLM.Util;

namespace SharperLLM.Agents;

public sealed class InMemoryPromptContext : IPromptContext
{
    private readonly PromptBuilder _template;
    private readonly List<(ChatMessage Message, PromptBuilder.From From)> _messages;

    public InMemoryPromptContext(PromptBuilder template)
    {
        _template = template.Clone();
        _messages = template.Messages
            .Select(static m => (CloneChatMessage(m.Item1), m.Item2))
            .ToList();
    }

    public PromptBuilder BuildPromptBuilder(IReadOnlyList<Tool> toolDefinitions)
    {
        var promptBuilder = _template.Clone();
        promptBuilder.Messages = _messages
            .Select(static m => (CloneChatMessage(m.Message), m.From))
            .ToArray();

        if (toolDefinitions.Count > 0)
        {
            promptBuilder.AvailableTools = toolDefinitions.ToList();
            promptBuilder.AvailableToolsFormatter ??= ToolPromptParser.Parse;
        }
        else
        {
            promptBuilder.AvailableTools = null;
        }

        return promptBuilder;
    }

    public void AppendUserMessage(string userInput)
    {
        _messages.Add((new ChatMessage { Content = userInput }, PromptBuilder.From.user));
    }

    public void AppendAssistantMessage(ChatMessage assistantMessage)
    {
        _messages.Add((CloneChatMessage(assistantMessage), PromptBuilder.From.assistant));
    }

    public void AppendToolResult(string toolCallId, string toolName, string toolResult)
    {
        _messages.Add((new ChatMessage
        {
            Content = toolResult,
            id = toolCallId,
            CustomProperties = new Dictionary<string, object>
            {
                ["tool_name"] = toolName
            }
        }, PromptBuilder.From.tool_result));
    }

    private static ChatMessage CloneChatMessage(ChatMessage original)
    {
        return new ChatMessage
        {
            Content = original.Content,
            ImageBase64 = original.ImageBase64,
            thinking = original.thinking,
            id = original.id,
            toolCalls = original.toolCalls?.Select(tc => new API.ToolCall
            {
                name = tc.name,
                id = tc.id,
                arguments = tc.arguments,
                index = tc.index
            }).ToList(),
            CustomProperties = original.CustomProperties != null
                ? new Dictionary<string, object>(original.CustomProperties)
                : null
        };
    }
}
