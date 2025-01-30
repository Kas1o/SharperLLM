using SharperLLM.Util;

namespace SharperLLM.API
{
	public class iLLMAPI//为啥不是接口? 因为奇怪的编译检查。
	{
		public virtual async IAsyncEnumerable<string> GenerateTextAsync(string prompt)
		{
			throw new NotImplementedException();
		}
		public virtual string GenerateText(string prompt, int retry = 0)
		{
			throw new NotImplementedException();
		}
		public virtual async IAsyncEnumerable<string> GenerateChatReplyAsync(PromptBuilder promptBuilder)
		{
			throw new NotImplementedException();
		}
		public virtual string GenerateChatReply(PromptBuilder promptBuilder)
		{
			throw new NotImplementedException();
		}
		public virtual ResponseEx GenereteEx()
		{
			throw new NotImplementedException();
		}
		public virtual async IAsyncEnumerable<ResponseEx> GenereteExAsync()
		{
			throw new NotImplementedException();
		}
	}
}
