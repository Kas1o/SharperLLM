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
		public List<ToolCall>? toolCallings { get; set; }
		public required FinishReason FinishReason { get; set; }
	}

	public enum FinishReason
	{
		Stop,
		Length,
		ContentFilter,
		FunctionCall,
	}

	public class ToolCall
	{
		public required string name { get; set; }
		public required string id { get; set; }
		public string? arguments { get; set; }//in json object
	}
}
