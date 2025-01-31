using System.Text;
namespace SharperLLM.Util;
public class PromptBuilder
{
    public enum From
    {
        user, assistant, system, tool_call, tool_result
    }
    public string SysSeqPrefix = "";
    public string System = "";
    public string SysSeqSuffix = "";
    public string ChatStart = "";
    public string InputPrefix = "";
    public string InputSuffix = "";
    public (string, From)[] Messages = { };
    public string? FirstOutputPrefix = null; //若存在,则在第一个输出前缀时使用
    public string OutputPrefix = "";
    public string OutputSuffix = "";
    public string? LatestOutputPrefix = null; //若存在,则在最后一个输出前缀时使用
    public string? ToolsPrefix = null;
    public string? ToolsSuffix = null;
    public string? ToolsCallPrefix = null;
    public string? ToolsCallSuffix = null;
    public string? ToolResultSeqPrefix = null;
    public string? ToolResultSeqSuffix = null;
    public PromptBuilder()
    {

    }
    public PromptBuilder(PromptBuilder template)
    {
        SysSeqPrefix = template.SysSeqPrefix;
        System = template.System;
        SysSeqSuffix = template.SysSeqSuffix;
        ChatStart = template.ChatStart;
        InputPrefix = template.InputPrefix;
        InputSuffix = template.InputSuffix;
        Messages = template.Messages.Select(a=>a).ToArray();
        FirstOutputPrefix = template.FirstOutputPrefix;
        OutputPrefix = template.OutputPrefix;
        OutputSuffix = template.OutputSuffix;
        LatestOutputPrefix = template.LatestOutputPrefix;
    }

	/// <summary>
	/// 兼容性保留，将被重定向至GeneratePromptWithLatestOuputPrefix
	/// </summary>
	/// <returns></returns>
	public string GetResult() => GeneratePromptWithLatestOuputPrefix();

    /// <summary>
    /// 生成只包含现有Messages的Prompt。 LatestOutputPrefix 将被忽略。
    /// </summary>
    /// <returns></returns>
    public string GenerateCleanPrompt()
    {
        StringBuilder resultBuilder = new StringBuilder();

        // 添加系统起始信息
        if (System != null)
        if (System.Trim() != String.Empty)
            resultBuilder.Append(SysSeqPrefix).Append(Environment.NewLine).Append(System).Append(SysSeqSuffix).Append(Environment.NewLine);

        bool isFirstOutput = true;

        foreach (var (message, from) in Messages)
        {
            switch (from)
            {
                case From.user:
                    resultBuilder.Append(InputPrefix).Append(Environment.NewLine)
                        .Append(message)
                        .Append(InputSuffix).Append(Environment.NewLine);
                    break;
                case From.assistant:
                    // 在第一个输出消息前添加FirstOutputPrefix
                    if (isFirstOutput && FirstOutputPrefix != null)
                    {
                        resultBuilder.Append(FirstOutputPrefix).Append(Environment.NewLine)
                            .Append(message)
                            .Append(OutputSuffix).Append(Environment.NewLine);
                        isFirstOutput = false;
                    }
                    else
                    {
                        resultBuilder.Append(OutputPrefix).Append(Environment.NewLine)
                            .Append(message)
                            .Append(OutputSuffix).Append(Environment.NewLine);
                    }
                    break;
                case From.system:
                    resultBuilder.Append(SysSeqPrefix).Append(Environment.NewLine)
                        .Append(message)
                        .Append(SysSeqSuffix);
                    break;
                default:
                    // 处理未知来源的消息类型，根据需要可以抛出异常或忽略
                    break;
            }
        }

        return resultBuilder.ToString();
    }

    /// <summary>
    /// 生成除了现有Messages外，还带有生成前缀的 Prompt。
    /// </summary>
    /// <returns></returns>
    public string GeneratePromptWithLatestOuputPrefix()
    {
        StringBuilder resultBuilder = new StringBuilder();

        // 添加系统起始信息
        if (System != null)
        if (System.Trim() != String.Empty)
            resultBuilder.Append(SysSeqPrefix).Append(Environment.NewLine).Append(System).Append(SysSeqSuffix).Append(Environment.NewLine);

        bool isFirstOutput = true;

        foreach (var (message, from) in Messages)
        {
            switch (from)
            {
                case From.user:
                    resultBuilder.Append(InputPrefix).Append(Environment.NewLine)
                        .Append(message)
                        .Append(InputSuffix).Append(Environment.NewLine);
                    break;
                case From.assistant:
                    // 在第一个输出消息前添加FirstOutputPrefix
                    if (isFirstOutput && FirstOutputPrefix != null)
                    {
                        resultBuilder.Append(FirstOutputPrefix).Append(Environment.NewLine)
                            .Append(message)
                            .Append(OutputSuffix).Append(Environment.NewLine);
                        isFirstOutput = false;
                    }
                    else
                    {
                        resultBuilder.Append(OutputPrefix).Append(Environment.NewLine)
                            .Append(message)
                            .Append(OutputSuffix).Append(Environment.NewLine);
                    }
                    break;
                case From.system:
                    resultBuilder.Append(SysSeqPrefix).Append(Environment.NewLine)
                        .Append(message)
                        .Append(SysSeqSuffix);
                    break;
                default:
                    // 处理未知来源的消息类型，根据需要可以抛出异常或忽略
                    break;
            }
        }
        resultBuilder.Append(!(LatestOutputPrefix == null) ? LatestOutputPrefix : OutputPrefix).Append(Environment.NewLine);

        return resultBuilder.ToString();
    }
	#region 一堆自带模板
	public static PromptBuilder ChatML => new PromptBuilder()
    {
        SysSeqPrefix = "<|im_start|>system",
        SysSeqSuffix = "<|im_end|>\n",
        InputPrefix = "<|im_start|>user",
        InputSuffix = "<|im_end|>\n",
        OutputPrefix = "<|im_start|>assistant",
        OutputSuffix = "<|im_end|>\n",
    };
    public static PromptBuilder Alpaca => new PromptBuilder()
    {
        SysSeqPrefix = "### Input:",
        InputPrefix = "### Instruction:",
        OutputPrefix = "### Response:"
    };
    public static PromptBuilder Mistral => new PromptBuilder()
    {
        SysSeqPrefix = "[INST]",
        SysSeqSuffix = "[/INST] ",
        InputPrefix = "[INST]",
        InputSuffix = "[/INST]"
    };
    public static PromptBuilder Mistralv7 => new PromptBuilder
    {
        SysSeqPrefix = "[SYSTEM_PROMPT]",
        SysSeqSuffix = "[/SYSTEM_PROMPT]",
        InputPrefix = "[INST]",
        InputSuffix = "[/INST]",
        ToolsPrefix = "[AVAILABLE_TOOLS]",
		ToolsSuffix = "[/AVAILABLE_TOOLS]",
        ToolsCallPrefix = "[TOOL_CALLS]",
        ToolsCallSuffix = "[TOOL_CALLS]</s>",
		ToolResultSeqPrefix = "[TOOL]",
        ToolResultSeqSuffix = "[/TOOL]",
        OutputSuffix = "</s>"
    };
	#endregion
	public PromptBuilder Clone() => new PromptBuilder(this);
}