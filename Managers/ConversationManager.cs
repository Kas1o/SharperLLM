using System;
using System.Collections.Generic;
using System.Text;
namespace LLMSharp.Managers;
public class ConversationManager
{
    public PromptBuilder _promptBuilder;
    private List<(string, PromptBuilder.From)> _messages;

    public ConversationManager(PromptBuilder promptBuilder)
    {
        _promptBuilder = promptBuilder;
        _messages = new List<(string, PromptBuilder.From)>();
    }

    public void AddSystemMessage(string message)
    {
        _messages.Add((message, PromptBuilder.From.system));
    }

    public void AddUserMessage(string message)
    {
        _messages.Add((message, PromptBuilder.From.user));
    }

    public void AddAssistantMessage(string message)
    {
        _messages.Add((message, PromptBuilder.From.assistant));
    }

    public virtual string GetPrompt()
    {
        _promptBuilder.Messages = _messages.ToArray();
        return _promptBuilder.GetResult();
    }
}
