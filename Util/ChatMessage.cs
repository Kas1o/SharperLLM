using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.Util
{
	public record struct ChatMessage()
    {
		public required string Content { get; set; } // The content of the message
        public string ImageBase64 { get; set; }

        public static implicit operator ChatMessage(string content)
		{
			return new ChatMessage
			{
				Content = content,
				ImageBase64 = "" // Default to empty string if no image is provided
			};
		}

		public static implicit operator string(ChatMessage message)
		{
			return message.Content;
		}
	}
}
