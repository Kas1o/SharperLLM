using SharperLLM.Util;
using System;
using System.Collections.Generic;
using System.Text;
namespace SharperLLM.Managers;
public class ConversationManager
{
    private PromptBuilder _promptBuilder;
    protected PromptBuilder promptBuilder
    {
        get {
            _promptBuilder.Messages = _messages.ToArray();
            return _promptBuilder;
        }
        set
        {
            _promptBuilder = value;
        }
    }
    private List<(ChatMessage, PromptBuilder.From)> _messages;

    public ConversationManager(PromptBuilder promptBuilder)
    {
        this._promptBuilder = promptBuilder;
        _messages = new();
    }
    /// <summary>
    /// Insert a Message at end of the conversation.
    /// </summary>
    /// <param name="message"></param>
    public void AddSystemMessage(string message)
    {
        _messages.Add((message, PromptBuilder.From.system));
    }
    /// <summary>
    /// Insert a Message at end of the conversation.
    /// </summary>
    /// <param name="message"></param>
    public void AddUserMessage(string message)
    {
        _messages.Add((message, PromptBuilder.From.user));
    }
    /// <summary>
    /// Insert a Message at end of the conversation.
    /// </summary>
    /// <param name="message"></param>
    public void AddAssistantMessage(string message)
    {
        _messages.Add((message, PromptBuilder.From.assistant));
    }
    /// <summary>
    /// Return the result prompt of this conversation.
    /// </summary>
    /// <returns></returns>
    public virtual PromptBuilder GeneratePromptBuilder()
    {
        return promptBuilder.Clone();
    }
}
