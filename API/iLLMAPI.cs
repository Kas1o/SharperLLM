﻿using SharperLLM.Util;

namespace SharperLLM.API
{
	public interface ILLMAPI
	{
		public IAsyncEnumerable<string> GenerateTextStream(string prompt);
		public Task<string> GenerateText(string prompt, int retry = 0);
		public IAsyncEnumerable<string> GenerateChatReplyStream(PromptBuilder promptBuilder);
		public Task<string> GenerateChatReply(PromptBuilder promptBuilder);
		public IAsyncEnumerable<ResponseEx> GenerateChatExStream(PromptBuilder pb);
		public Task<ResponseEx> GenerateChatEx(PromptBuilder pb);
	}
}
