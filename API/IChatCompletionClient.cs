using SharperLLM.Util;

namespace SharperLLM.API
{
	public interface IChatCompletionClient
	{
		IAsyncEnumerable<ResponseEx> GenerateStreamAsync(PromptBuilder pb, CancellationToken cancellationToken);
		Task<ResponseEx> GenerateAsync(PromptBuilder pb);
	}
}
