using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.Util
{
	public class CustomPromptBuilder
	{
		public List<(CustomRole cr, string content)> Messages = new();

		public string GenerateCleanPrompt()
		{
			var sb = new StringBuilder();
			foreach (var message in Messages)
			{
				sb
				.Append(message.cr.StartSequence)
				.Append(message.content)
				.Append(message.cr.EndSequence);
			}
			return sb.ToString();
		}

		public string GeneratePromptWithGenerationPrefix(CustomRole generationRole)
		{
			return GenerateCleanPrompt() + generationRole.StartSequence;
		}
	}
}
