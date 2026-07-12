namespace SharperLLM.API
{
	public interface ITextCompletionClient
	{
		IAsyncEnumerable<string> GenerateStreamAsync(string prompt, CancellationToken cancellationToken);
		Task<string> GenerateAsync(string prompt);
	}
}
