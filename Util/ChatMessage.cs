using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.Util
{
	public class ChatMessage
	{
		public string Content { get; private set; }// The content of the message
		public string ImageBase64 { get; private set; }

		public static implicit operator ChatMessage(string content)
		{
			return new ChatMessage
			(
				content,
				null
			);
		}


		[Newtonsoft.Json.JsonConstructor]
		public ChatMessage(string Content, string ImageBase64)
		{
			this.Content = Content;
			this.ImageBase64 = ImageBase64;
		}

		public static implicit operator string(ChatMessage message)
		{
			return message.Content;
		}
	}
}
