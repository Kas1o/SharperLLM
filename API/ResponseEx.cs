using SharperLLM.FunctionCalling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.API
{
	public class ResponseEx
	{
		public required string content { get; set; }
		public string thinking { get; set; } = null;
		public List<ToolCall>? toolCallings { get; set; }
		public required FinishReason FinishReason { get; set; }

		public static ResponseEx operator+(ResponseEx prev, ResponseEx next)
		{
			var combined = new ResponseEx
			{
				content = prev.content + next.content,
				thinking = (prev.thinking ?? "") + (next.thinking ?? ""),
				FinishReason = next.FinishReason,
				toolCallings = new List<ToolCall>()
			};
			if (prev.toolCallings != null)
			{
				combined.toolCallings.AddRange(prev.toolCallings);
			}
			if (next.toolCallings != null)
			{
				combined.toolCallings.AddRange(next.toolCallings);
			}
			return combined;
		}
	}

	public enum FinishReason
	{
		Stop,
		Length,
		ContentFilter,
		FunctionCall,
		None
	}

	public class ToolCall
	{
		public required string name { get; set; }
		public required string id { get; set; }
		public string? arguments { get; set; }//in json object
		public int index { get; set; }
	}
}
