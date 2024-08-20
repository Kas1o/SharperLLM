# SharperLLM

一个锐利的用于C# dotnet的 LLM 工具集类库。

---


## 功能支持列表
### 概览：
* 连接多种LLM API
* 对LLM进行测试
* 读取导出数据集（待实现）
### 列表：
|功能            |  当前状况|
|----------------|---------|
|OpenAI对话API   |同步、异步|
|OpenAI文本补全API|未支持   |
|特定API支持      |进行中   |
|数据集导出       |未支持   |
|模型能力测试     |进行中   |
|数据集导出       |未支持   |
|数据集导入       |未支持   |



## API示例
```csharp
Console.WriteLine("openai api 地址 (example:http://localhost:5001/v1):");
var url = Console.ReadLine();
OpenAIAPI api = new(url, "sk-4389a878240d40aa91c22b9ad72732c8", "gpt114514-Pro-Max-Ultra-Extreme");

PromptBuilder pb = new PromptBuilder
{
    Messages =
    [
        ("你是一个强大的人工智能。",PromptBuilder.From.system),
        ("我去，人工智能！",PromptBuilder.From.user),
        ("好神奇啊！",PromptBuilder.From.user),
    ]
};

Console.WriteLine(api.GenerateChatReply(pb));
```

## 如何使用？
1. 克隆本仓库到本地。
```
git clone https://github.com/Kas1o/SharperLLM
```
2. 如果没有，创建自己的C#项目。
```
dotnet new console #如果你需要的是控制台应用的话。
```
3. 引用此项目到你的项目。
   
浏览到你的项目文件 `*.csproj`中，加入以下语句。
```diff
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

+  <ItemGroup>
+    <ProjectReference Include="path/to/your/SharperLLM/SharperLLM.csproj" />
+  </ItemGroup>

</Project>
```

4. 开始使用SharperLLM
