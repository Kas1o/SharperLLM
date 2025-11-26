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
			
			// 创建一个字典来存储按index分组的工具调用
			var toolCallDict = new Dictionary<int, ToolCall>();
			
			// 先添加prev中的工具调用
			if (prev.toolCallings != null)
			{
				foreach (var toolCall in prev.toolCallings)
				{
					toolCallDict[toolCall.index] = new ToolCall
					{
						name = toolCall.name,
						id = toolCall.id,
						arguments = toolCall.arguments ?? "",
						index = toolCall.index
					};
				}
			}
			
			// 处理next中的工具调用，同index的进行内容累积
			if (next.toolCallings != null)
			{
				foreach (var toolCall in next.toolCallings)
				{
					if (toolCallDict.TryGetValue(toolCall.index, out var existingToolCall))
					{
						// 同index的工具调用，累积arguments内容
						existingToolCall.arguments = (existingToolCall.arguments ?? "") + (toolCall.arguments ?? "");
					}
					else
					{
						// 新的工具调用，直接添加
						toolCallDict[toolCall.index] = new ToolCall
						{
							name = toolCall.name,
							id = toolCall.id,
							arguments = toolCall.arguments ?? "",
							index = toolCall.index
						};
					}
				}
			}
			
			// 将字典中的工具调用按index排序后添加到结果中
			combined.toolCallings.AddRange(toolCallDict.Values.OrderBy(tc => tc.index));
			
			return combined;
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
