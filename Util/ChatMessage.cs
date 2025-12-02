using Newtonsoft.Json;
using SharperLLM.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.Util
{
	public class ChatMessage : ICloneable
	{
		public required string Content { get; set; }// The content of the message
		public string? ImageBase64 { get; set; } = null;

		public string? thinking { get; set; } = null;
		public string? id { get; set; } = null;
		public List<ToolCall>? toolCalls { get; set; } = null;

		public static implicit operator ChatMessage(string content)
		{
			return new ChatMessage { Content = content };
		}
		public static implicit operator string(ChatMessage message)
		{
			return message.Content;
		}

		public override string ToString()
		{
			return ((ImageBase64 != null) ? "[image]" : "" )+ $"{Content}";
		}

		public object Clone()
		{
			var cloned = (ChatMessage)this.MemberwiseClone();
			return cloned;
		}
	}
}
