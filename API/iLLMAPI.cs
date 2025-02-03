using SharperLLM.Util;

namespace SharperLLM.API
{
	public interface iLLMAPI//为啥不是接口? 因为奇怪的编译检查。
	{
		public async IAsyncEnumerable<string> GenerateTextStream(string prompt)
		{
			throw new NotImplementedException();
		}
		public virtual string GenerateText(string prompt, int retry = 0)
		{
			throw new NotImplementedException();
		}
		public virtual async IAsyncEnumerable<string> GenerateChatReplyStream(PromptBuilder promptBuilder)
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
		public virtual async IAsyncEnumerable<ResponseEx> GenereteExStream()
		{
			throw new NotImplementedException();
		}
	}
}
