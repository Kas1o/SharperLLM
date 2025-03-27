using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.FunctionCalling
{
	public static class ToolPromptParser
	{
		public static string Parse(List<Tool> tools)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("available tools:");
			sb.AppendLine(JsonConvert.SerializeObject(tools,Formatting.Indented));
			return sb.ToString();
		}
	}
}
