using SharperLLM.Util;

namespace SharperLLM.API
{
	/// <summary>
	/// 将 ITextCompletionClient 包装为 IChatCompletionClient。
	/// 使用 PromptBuilder 模板将对话消息拼接为文本 prompt，再调用文本补全客户端。
	/// 不支持 tool calling。
	/// </summary>
	public class TextToChatAdapter : IChatCompletionClient
	{
		private readonly ITextCompletionClient _client;
		private readonly PromptBuilder _template;

		public TextToChatAdapter(ITextCompletionClient client, PromptBuilder template)
		{
			_client = client;
			_template = template;
		}

		public async Task<ResponseEx> GenerateAsync(PromptBuilder pb)
		{
			var prompt = BuildPrompt(pb);
			var text = await _client.GenerateAsync(prompt);
			return new ResponseEx
			{
				Body = new ChatMessage { Content = text },
				FinishReason = FinishReason.Stop
			};
		}

		public async IAsyncEnumerable<ResponseEx> GenerateStreamAsync(
			PromptBuilder pb,
			[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
		{
			var prompt = BuildPrompt(pb);
			await foreach (var chunk in _client.GenerateStreamAsync(prompt, cancellationToken))
			{
				yield return new ResponseEx
				{
					Body = new ChatMessage { Content = chunk },
					FinishReason = FinishReason.None
				};
			}
			yield return new ResponseEx
			{
				Body = new ChatMessage { Content = "" },
				FinishReason = FinishReason.Stop
			};
		}

		private string BuildPrompt(PromptBuilder pb)
		{
			var merged = _template.Clone();
			merged.Messages = pb.Messages;
			merged.System = pb.System;
			if (pb.AvailableTools != null)
				merged.AvailableTools = pb.AvailableTools;
			return merged.GeneratePromptWithLatestOuputPrefix();
		}
	}
}
