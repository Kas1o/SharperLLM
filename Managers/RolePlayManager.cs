using SharperLLM.Util;
using System;
using System.Collections.Generic;


namespace SharperLLM.Managers;

public class RoleplayManager : ConversationManager
{
    private string _userRoleName;
    private string _assistantRoleName;
    public RoleplayManager(PromptBuilder promptBuilder, string userRoleName, string assistantRoleName)
        : base(promptBuilder)
    {
        _userRoleName = userRoleName;
        _assistantRoleName = assistantRoleName;
        ModifyPrefixes();
    }

    public List<(string message, int depth, PromptBuilder.From from)> InsertMessages = new();

    private void ModifyPrefixes()
    {
        // 修改InputPrefix和OutputPrefix，添加角色名称
            base.promptBuilder.InputPrefix += $"【{_userRoleName??"user"}】";
            base.promptBuilder.OutputPrefix += $"【{_assistantRoleName??"assistant"}】";
    }
    public override PromptBuilder GetPromptBuilder()
    {
        PromptBuilder promptBuilder = base.promptBuilder;
        var mes = promptBuilder.Messages.ToList();
        foreach (var item in InsertMessages)
        {
            mes.Insert(mes.Count - item.depth, (item.message, item.from));
        }
        mes = mes.Select(a => (ReplaceMacros(a.Item1),a.Item2)).ToList();
        promptBuilder.Messages = mes.ToArray();
        return promptBuilder;
    }
    private string ReplaceMacros(string text)
    {
        if (!string.IsNullOrEmpty(_userRoleName))
        {
            text = text.Replace("{{user}}", _userRoleName);
        }
        if (!string.IsNullOrEmpty(_assistantRoleName))
        {
            text = text.Replace("{{char}}", _assistantRoleName);
        }
        return text;
    }
}
