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

    private void ModifyPrefixes()
    {
        // 修改InputPrefix和OutputPrefix，添加角色名称
            base.promptBuilder.InputPrefix += $"【{_userRoleName??"用户"}】";
            base.promptBuilder.OutputPrefix += $"【{_assistantRoleName??"AI助手"}】";
    }
    public override string GetPrompt()
    {
        string prompt = base.GetPrompt();
        prompt = ReplaceMacros(prompt);
        return prompt;
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
