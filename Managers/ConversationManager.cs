﻿using System;
using System.Collections.Generic;
using System.Text;
namespace SharperLLM.Managers;
public class ConversationManager
{
    public PromptBuilder promptBuilder;
    private List<(string, PromptBuilder.From)> _messages;

    public ConversationManager(PromptBuilder promptBuilder)
    {
        this.promptBuilder = promptBuilder;
        _messages = new List<(string, PromptBuilder.From)>();
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
    public virtual string GetPrompt()
    {
        promptBuilder.Messages = _messages.ToArray();
        return promptBuilder.GetResult();
    }
}
