﻿using SharperLLM.FunctionCalling;
using SharperLLM.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.API
{
	public class ResponseEx
	{
		public required ChatMessage Body { get; set; }
		public required FinishReason FinishReason { get; set; }

		public static ResponseEx operator+(ResponseEx prev, ResponseEx next)
		{
			var combined = new ResponseEx
			{
				Body = new ChatMessage
				{
					Content = prev.Body.Content + next.Body.Content,
					thinking = (prev.Body.thinking != null || next.Body.thinking != null) ? (prev.Body.thinking ?? "") + (next.Body.thinking ?? "") : null, 
					id = next.Body.id,// 以next的id为准
					toolCalls = new List<ToolCall>()
				},
				FinishReason = next.FinishReason,
			};
			
			// 创建一个字典来存储按index分组的工具调用
			var toolCallDict = new Dictionary<int, ToolCall>();
			
			// 先添加prev中的工具调用
			if (prev.Body.toolCalls != null)
			{
				foreach (var toolCall in prev.Body.toolCalls)
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
			if (next.Body.toolCalls != null)
			{
				foreach (var toolCall in next.Body.toolCalls)
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
			combined.Body.toolCalls.AddRange(toolCallDict.Values.OrderBy(tc => tc.index));
			
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
