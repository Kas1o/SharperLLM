using SharperLLM.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.Util
{
    public class ToolCallChatMessage : ChatMessage
	{
		public ToolCallChatMessage( List<ToolCall> _toolCall)
			: base(null, null)
		{
			this.toolCalls = _toolCall;
		}
		public int index;
		public string id;
		public List<ToolCall> toolCalls;
    }
}
