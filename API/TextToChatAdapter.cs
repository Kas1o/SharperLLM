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
			// 文本补全端点只能接收纯文本：图像必须显式报错，
			// 否则会被悄悄丢掉，表现为「模型看不到图」这种难以排查的问题。
			var withImage = pb.Messages.FirstOrDefault(m => m.Item1.Images is { Count: > 0 });
			if (withImage.Item1 != null)
				throw new NotSupportedException(
					"The current API is a text-completion endpoint and cannot accept image input. " +
					"Remove the attached image or switch to an OpenAI-compatible chat API.");

			var merged = _template.Clone();
			merged.Messages = pb.Messages;
			merged.System = pb.System;
			if (pb.AvailableTools != null)
				merged.AvailableTools = pb.AvailableTools;
			return merged.GeneratePromptWithLatestOuputPrefix();
		}
	}
}
