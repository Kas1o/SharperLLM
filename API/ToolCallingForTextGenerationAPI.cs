using SharperLLM.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.API
{
	public class ToolCallingForTextGenerationAPI(ILLMAPI _origin) : ILLMAPI
	{
		ILLMAPI origin = _origin;

		Task<string> ILLMAPI.GenerateChatReply(PromptBuilder promptBuilder)
		{
			throw new NotImplementedException();
		}

		IAsyncEnumerable<string> ILLMAPI.GenerateChatReplyStream(PromptBuilder promptBuilder)
		{
			throw new NotImplementedException();
		}

		Task<string> ILLMAPI.GenerateText(string prompt, int retry)
		{
			throw new NotImplementedException();
		}

		IAsyncEnumerable<string> ILLMAPI.GenerateTextStream(string prompt)
		{
			throw new NotImplementedException();
		}

		Task<ResponseEx> ILLMAPI.GenereteEx()
		{
			throw new NotImplementedException();
		}

		IAsyncEnumerable<ResponseEx> ILLMAPI.GenereteExStream()
		{
			throw new NotImplementedException();
		}
	}
}
