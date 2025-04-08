using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.FunctionCalling
{
	public class Tool
	{
		public required string name { get; set; }
		public required string description { get; set; }
		public List<(ToolParameter parameter,bool required)>? parameters { get; set; }
	}

	public class ToolParameter
	{
		public ParameterType type;
		public string description;
		public string name;
		public List<string> @enum;
	}

	public enum ParameterType
	{
		String,
		Number,
		Boolean,
		Array,
		Object
	}
}
