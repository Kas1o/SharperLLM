using SharperLLM.Util;

namespace SharperLLM.API
{
	public interface ILLMAPI
	{
		public IAsyncEnumerable<string> GenerateTextStream(string prompt, CancellationToken cancellationToken);
		public Task<string> GenerateText(string prompt, int retry = 0);
		public IAsyncEnumerable<string> GenerateChatReplyStream(PromptBuilder promptBuilder, CancellationToken cancellationToken);
		public Task<string> GenerateChatReply(PromptBuilder promptBuilder);
		public IAsyncEnumerable<ResponseEx> GenerateChatExStream(PromptBuilder pb, CancellationToken cancellationToken);
		public Task<ResponseEx> GenerateChatEx(PromptBuilder pb);
	}
}
