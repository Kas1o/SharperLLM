using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.API
{
	public class ResponseEx
	{
		public string content;
		public List<ToolCalling> toolCallings;
	}

	public class ToolCalling
	{
		public string tool;
		public string arguments;
	}
}
