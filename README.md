# SharperLLM

一个锐利的用于C# dotnet的LLM工具集类库。

---

## 功能支持列表
### 概览：
- 连接多种LLM API
- 对LLM进行测试
- 读取导出多种类型数据集
- 启动OpenAI兼容服务器

### 列表：
| 功能 | 当前状况 |
| --- | --- |
| OpenAI对话API | 支持、流式 |
| OpenAI文本补全API | 支持 |
| 特定API支持（如Ollama、Kobold） | 进行中 |
| 数据集导出 | ShareGPT |
| 模型能力测试 | 进行中 |
| 数据集导入 | ShareGPT、Alpaca |

## API示例

### OpenAI API示例
```csharp
Console.WriteLine("openai api 地址 (example:http://localhost:5001/v1):");
var url = Console.ReadLine();
OpenAIAPI api = new(url, "sk-4389a878240d40aa91c22b9ad72732c8", "gpt114514-Pro-Max-Ultra-Extreme");

PromptBuilder pb = new PromptBuilder
{
    Messages =
    [
        ("你是一个强大的人工智能。", PromptBuilder.From.system),
        ("我去，人工智能！", PromptBuilder.From.user),
        ("好神奇啊！", PromptBuilder.From.user),
    ]
};

Console.WriteLine(api.GenerateChatReply(pb));
```

### Ollama API示例
```csharp
OllamaAPI ollamaApi = new("http://localhost:114514", "your_model_name");

PromptBuilder pb = new PromptBuilder(PromptBuilder.ChatML)
{
    Messages =
    [
        ("你是一个强大的人工智能。", PromptBuilder.From.system),
        ("我去，人工智能！", PromptBuilder.From.user),
        ("好神奇啊！", PromptBuilder.From.user),
    ]
};

string result = await ollamaApi.GenerateText(pb.GeneratePromptWithLatestOuputPrefix());
Console.WriteLine(result);
```

### Kobold API示例
```csharp
KoboldAPI koboldApi = new("http://localhost:5000");

PromptBuilder pb = new PromptBuilder(PromptBuilder.ChatML)
{
    Messages =
    [
        ("你是一个强大的人工智能。", PromptBuilder.From.system),
        ("我去，人工智能！", PromptBuilder.From.user),
        ("好神奇啊！", PromptBuilder.From.user),
    ]
};

string result = await koboldApi.GenerateText(pb.GeneratePromptWithLatestOuputPrefix());
Console.WriteLine(result);
```

## 如何使用？
1. 克隆本仓库到本地。
```bash
git clone https://github.com/Kas1o/SharperLLM
```
2. 如果没有，创建自己的C#项目。
```bash
dotnet new console # 如果你需要的是控制台应用的话。
```
3. 引用此项目到你的项目。

浏览到你的项目文件 `*.csproj` 中，加入以下语句。
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="path/to/your/SharperLLM/SharperLLM.csproj" />
  </ItemGroup>

</Project>
```

4. 开始使用SharperLLM

以下是一个简单的使用示例，展示如何使用OpenAI API进行对话：
```csharp
using SharperLLM.API;
using SharperLLM.Util;
using System;
using System.Threading.Tasks;

namespace SharperLLMExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("请输入OpenAI API地址 (example:http://localhost:5001/v1):");
            var url = Console.ReadLine();
            Console.WriteLine("请输入OpenAI API密钥:");
            var apiKey = Console.ReadLine();
            Console.WriteLine("请输入要使用的模型名称:");
            var model = Console.ReadLine();

            OpenAIAPI api = new OpenAIAPI(url, apiKey, model);

            PromptBuilder pb = new PromptBuilder
            {
                System = "你是一个友好且有用的人工智能助手。",
                Messages = Array.Empty<(ChatMessage, PromptBuilder.From)>()
            };

            Console.WriteLine("对话开始！输入 '退出' 结束对话。");

            while (true)
            {
                Console.Write("你: ");
                string userInput = Console.ReadLine();

                if (userInput == "退出")
                {
                    break;
                }

                pb.Messages = pb.Messages.Append((userInput, PromptBuilder.From.user)).ToArray();

                try
                {
                    string response = await api.GenerateChatReply(pb);
                    Console.WriteLine($"助手: {response}");
                    pb.Messages = pb.Messages.Append((response, PromptBuilder.From.assistant)).ToArray();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发生错误: {ex.Message}");
                }
            }

            Console.WriteLine("对话结束。");
        }
    }
}
```

## 工具使用功能（Tool Use）
SharperLLM支持工具使用功能，允许AI调用特定工具来完成任务，例如访问本地文件。以下是一个示例，展示如何使用工具调用功能：

```csharp
using SharperLLM.API;
using SharperLLM.FunctionCalling;
using SharperLLM.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SharperLLMCommandLineApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 配置OpenAI API
            Console.WriteLine("请输入OpenAI API地址 (example:http://localhost:5001/v1):");
            var url = Console.ReadLine();
            Console.WriteLine("请输入OpenAI API密钥:");
            var apiKey = Console.ReadLine();
            Console.WriteLine("请输入要使用的模型名称:");
            var model = Console.ReadLine();

            OpenAIAPI api = new OpenAIAPI(url, apiKey, model);

            // 定义文件访问工具
            var fileAccessTool = new Tool
            {
                name = "FileAccessTool",
                description = "用于读取本地文件的内容",
                parameters = new List<(ToolParameter parameter, bool required)>
                {
                    (new ToolParameter
                    {
                        type = ParameterType.String,
                        description = "要读取的文件路径",
                        name = "filePath",
                        @enum = null
                    }, true)
                }
            };

            // 初始化PromptBuilder
            PromptBuilder pb = new PromptBuilder
            {
                System = "你是一个友好且有用的人工智能助手。可以使用FileAccessTool工具读取本地文件内容。",
                Messages = Array.Empty<(ChatMessage, PromptBuilder.From)>(),
                AvailableTools = new List<Tool> { fileAccessTool },
                AvailableToolsFormatter = ToolPromptParser.Parse
            };

            Console.WriteLine("对话开始！输入 '退出' 结束对话。");

            while (true)
            {
                // 获取用户输入
                Console.Write("你: ");
                string userInput = Console.ReadLine();

                if (userInput == "退出")
                {
                    break;
                }

                // 添加用户消息到PromptBuilder
                pb.Messages = pb.Messages.Append((userInput, PromptBuilder.From.user)).ToArray();

                try
                {
                    // 调用API获取回复
                    var responseEx = await api.GenerateChatEx(pb);

                    if (responseEx.FinishReason == FinishReason.FunctionCall)
                    {
                        foreach (var toolCall in responseEx.toolCallings)
                        {
                            if (toolCall.name == "FileAccessTool")
                            {
                                var arguments = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(toolCall.arguments);
                                if (arguments.TryGetValue("filePath", out string filePath))
                                {
                                    try
                                    {
                                        string fileContent = File.ReadAllText(filePath);
                                        var toolResultMessage = new ToolChatMessage(fileContent, toolCall.id);
                                        pb.Messages = pb.Messages.Append((toolResultMessage, PromptBuilder.From.tool_result)).ToArray();

                                        // 再次调用API获取回复
                                        responseEx = await api.GenerateChatEx(pb);
                                    }
                                    catch (Exception ex)
                                    {
                                        var errorMessage = $"读取文件时发生错误: {ex.Message}";
                                        var toolResultMessage = new ToolChatMessage(errorMessage, toolCall.id);
                                        pb.Messages = pb.Messages.Append((toolResultMessage, PromptBuilder.From.tool_result)).ToArray();

                                        // 再次调用API获取回复
                                        responseEx = await api.GenerateChatEx(pb);
                                    }
                                }
                            }
                        }
                    }

                    // 输出回复
                    Console.WriteLine($"助手: {responseEx.content}");

                    // 添加助手回复到PromptBuilder
                    pb.Messages = pb.Messages.Append((responseEx.content, PromptBuilder.From.assistant)).ToArray();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发生错误: {ex.Message}");
                }
            }

            Console.WriteLine("对话结束。");
        }
    }
}
```

## 贡献
如果你想为SharperLLM项目做出贡献，请遵循以下步骤：
1. Fork本仓库。
2. 创建一个新的分支：`git checkout -b feature/your-feature-name`。
3. 提交你的更改：`git commit -m "Add your feature description"`。
4. 推送你的分支：`git push origin feature/your-feature-name`。
5. 提交一个Pull Request。

## 许可证
SharperLLM采用MIT许可证。详情请参阅 [LICENSE.txt](LICENSE.txt) 文件。
