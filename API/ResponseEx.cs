﻿using SharperLLM.FunctionCalling;
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
		public List<Tool> toolCallings;
	}
}
