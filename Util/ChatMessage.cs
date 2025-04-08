using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.Util
{
	public class ChatMessage(string _content, string _imageBase64 = null)
	{
		public string Content { get; private set; } = _content;// The content of the message
		public string ImageBase64 { get; private set; } = _imageBase64;

		public static implicit operator ChatMessage(string content)
		{
			return new ChatMessage
			(
				_content : content,
				_imageBase64 : "" // Default to empty string if no image is provided
			);
		}

		public static implicit operator string(ChatMessage message)
		{
			return message.Content;
		}
	}
}
