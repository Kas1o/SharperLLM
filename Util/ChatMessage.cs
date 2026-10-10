﻿using Newtonsoft.Json;
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

		/// <summary>
		/// 多模态图像内容块（可包含多张图像，支持内联 Base64 或文件引用）。
		/// </summary>
		public List<ChatImage>? Images { get; set; } = null;

		/// <summary>
		/// 旧版单图字段，读写均映射到 <see cref="Images"/> 的第一张图像（内联 Base64 部分）。
		/// 新代码请直接使用 <see cref="Images"/>。
		/// </summary>
		[JsonIgnore]
		public string? ImageBase64
		{
			get => Images is { Count: > 0 } ? Images[0].Base64 : null;
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					Images = null;
					return;
				}
				Images = new List<ChatImage> { ChatImage.FromBase64(value!, ChatImage.DefaultMimeType) };
			}
		}

		public string? thinking { get; set; } = null;
		public string? id { get; set; } = null;
		public List<ToolCall>? toolCalls { get; set; } = null;

		/// <summary>
		/// 消息级别的自定义参数，会被合并到请求体的 message 对象中
		/// </summary>
		public Dictionary<string, object>? CustomProperties { get; set; } = null;

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
			var imageTag = Images is { Count: > 0 }
				? (Images.Count == 1 ? "[image]" : $"[{Images.Count} images]")
				: "";
			return imageTag + $"{Content}";
		}

		public object Clone()
		{
			var cloned = (ChatMessage)this.MemberwiseClone();
			cloned.Images = Images?.Select(i => (ChatImage)i.Clone()).ToList();
			return cloned;
		}
	}
}
