using SharperLLM.FunctionCalling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.API
{
	public class ResponseEx
	{
		public required string content { get; set; }
		public string thinking { get; set; } = null;
		public List<ToolCall>? toolCallings { get; set; }
		public required FinishReason FinishReason { get; set; }

		public static ResponseEx operator+(ResponseEx prev, ResponseEx next)
		{
			var combined = new ResponseEx
			{
				content = prev.content + next.content,
				thinking = (prev.thinking ?? "") + (next.thinking ?? ""),
				FinishReason = next.FinishReason,
				toolCallings = new List<ToolCall>()
			};
			
			// 创建一个字典来存储按id分组的工具调用
			var toolCallDict = new Dictionary<string, ToolCall>();
			
			// 先添加prev中的工具调用
			if (prev.toolCallings != null)
			{
				foreach (var toolCall in prev.toolCallings)
				{
					toolCallDict[toolCall.id] = new ToolCall
					{
						name = toolCall.name,
						id = toolCall.id,
						arguments = toolCall.arguments ?? "",
						index = toolCall.index
					};
				}
			}
			
			// 处理next中的工具调用，实现正确的参数累积
			if (next.toolCallings != null)
			{
				foreach (var toolCall in next.toolCallings)
				{
					if (toolCallDict.TryGetValue(toolCall.id, out var existingToolCall))
					{
						// 同id的工具调用，需要正确累积参数
						if (!string.IsNullOrEmpty(toolCall.arguments))
						{
							// 对于流式生成，我们需要智能处理参数累积
							// 如果next的参数看起来是完整的JSON格式且比现有参数更长，我们认为它包含了累积结果
							// 否则，我们保留现有参数，因为简单拼接可能导致JSON格式错误
							if (IsValidJsonStart(toolCall.arguments) && toolCall.arguments.Length > existingToolCall.arguments.Length)
							{
								existingToolCall.arguments = toolCall.arguments;
							}
							// 否则保留现有参数，避免错误累积
						}
					}
					else
					{
						// 新的工具调用，直接添加
						toolCallDict[toolCall.id] = new ToolCall
						{
							name = toolCall.name,
							id = toolCall.id,
							arguments = toolCall.arguments ?? "",
							index = toolCall.index
						};
					}
				}
			}
			
			// 将字典中的工具调用添加到结果中
			combined.toolCallings.AddRange(toolCallDict.Values);
			
			return combined;
		}
		
		/// <summary>
		/// 简单检查字符串是否可能是有效的JSON开始
		/// </summary>
		private static bool IsValidJsonStart(string str)
		{
			// 去除前后空白字符
			str = str.Trim();
			// 检查是否以{开头且可能包含有效的JSON结构
			return str.StartsWith("{") && (str.Contains(":") || str.Length <= 1);
		}
	}

	public enum FinishReason
	{
		Stop,
		Length,
		ContentFilter,
		FunctionCall,
		None
	}

	public class ToolCall
	{
		public required string name { get; set; }
		public required string id { get; set; }
		public string? arguments { get; set; }//in json object
		public int index { get; set; }
	}
}
