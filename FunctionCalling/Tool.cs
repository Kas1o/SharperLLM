using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.FunctionCalling
{
	public class Tool
	{
		public string name { get; set; }
		public Dictionary<string, string> arguments { get; set; } = new();
	}
}
