using System.Text;
namespace SharperLLM.Util;
public class PromptBuilder
{
    public enum From
    {
        user, assistant, system
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

    public string GetResult()
    {
        StringBuilder resultBuilder = new StringBuilder();

        // 添加系统起始信息
        if(System.Trim() != String.Empty)
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

    public PromptBuilder Clone() => new PromptBuilder
    {
        SysSeqPrefix = this.SysSeqPrefix,
        System = this.System,
        SysSeqSuffix = this.SysSeqSuffix,
        ChatStart = this.ChatStart,
        InputPrefix = this.InputPrefix,
        InputSuffix = this.InputSuffix,
        Messages = this.Messages,
        FirstOutputPrefix = this.FirstOutputPrefix,
        OutputPrefix = this.OutputPrefix,
        OutputSuffix = this.OutputSuffix,
        LatestOutputPrefix = this.LatestOutputPrefix,

    };
}