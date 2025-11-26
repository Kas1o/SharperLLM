using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharperLLM.FunctionCalling;
using SharperLLM.Util;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace SharperLLM.API
{
	/// <summary>
	/// example url: "http://api.openai.com/v1"
	/// </summary>
	public class OpenAIAPI(string _url, string _apiKey, string _model, float _temperature = 0.7f, int _max_tokens = 8192) : ILLMAPI
	{
		public string url = _url;
		public string apiKey = _apiKey;
		public string model = _model;
		public float temperature = _temperature;
		public int max_tokens = _max_tokens;

		#region basic api
		public async Task<ResponseEx> GenerateChatEx(PromptBuilder pb)
		{
			var targetURL = $"{url}/chat/completions";
			var messages = BuildMessages(pb.Messages);
			var requestBody = new
			{
				model,
				messages,
				temperature,
				max_tokens,
				tools = pb.AvailableTools != null? BuildTools(pb.AvailableTools): null,
				stream = false
			};

			using var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
			//序列化且忽略null字段
			var jsonContent = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
			var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
			var response = await client.PostAsync(targetURL, content);
			var responseString = await response.Content.ReadAsStringAsync();
			/*
			   {
			    "id": "ac61b97e-9c11-41c2-a1be-f4424509cd38",
			    "object": "chat.completion",
			    "created": 1744135577,
			    "model": "deepseek-chat",
			    "choices": [
			        {
			            "index": 0,
			            "message": {
			                "role": "assistant",
			                "content": "",
			                "tool_calls": [ //sometimes this is a object, sometimes this is a array
			                    {
			                        "index": 0,
			                        "id": "call_0_21c477c2-0997-4826-a125-c5f9daf02fc9",
			                        "type": "function",
			                        "function": {
			                            "name": "example_tool_name",
			                            "arguments": "{\"example_string\": \"First call to the example tool.\"}"
			                        }
			                    },
			                    {
			                        "index": 1,
			                        "id": "call_1_77dac30d-cd14-417e-9b00-d9375dd5e903",
			                        "type": "function",
			                        "function": {
			                            "name": "example_tool_name",
			                            "arguments": "{\"example_string\": \"Second call to the example tool.\"}"
			                        }
			                    }
			                ]
			            },
			            "logprobs": null,
			            "finish_reason": "tool_calls"
			        }
			    ],
			    "usage": {
			        "prompt_tokens": 126,
			        "completion_tokens": 55,
			        "total_tokens": 181,
			        "prompt_tokens_details": {
			            "cached_tokens": 64
			        },
			        "prompt_cache_hit_tokens": 64,
			        "prompt_cache_miss_tokens": 62
			    },
			    "system_fingerprint": "fp_3d5141a69a_prod0225"
			}
			 */
			if (response.IsSuccessStatusCode)
			{
				JObject jsonResponse = JObject.Parse(responseString);

				// Extract tool_calls and finish_reason
				var toolCalls = new List<ToolCall>();
				var isToolsArray = jsonResponse["choices"]?[0]?["message"]?["tool_calls"] is JArray ;
				if (isToolsArray)
				{
					foreach (var item in jsonResponse["choices"]?[0]?["message"]?["tool_calls"])
					{
						toolCalls.Add(new ToolCall
						{
							id = item["id"].ToString(),
							name = item["function"]["name"].ToString(),
							arguments = item["function"]["arguments"]?.ToString(),
							index = item["index"]?.ToObject<int>() ?? 0
						});
					}
				}
				else
				{
					// 再判断是否存在tool_calls
					if (jsonResponse["choices"]?[0]?["message"]?	["tool_calls"] != null)
						toolCalls.Add(new ToolCall
						{
							id = jsonResponse["choices"][0]["message"]["tool_calls"]["id"].ToString(),
							name = jsonResponse["choices"][0]["message"]["tool_calls"]["function"]["name"].ToString(),
							arguments = jsonResponse["choices"][0]["message"]["tool_calls"]["function"]["arguments"]?.ToString()
						});
				}

				var finishReason = jsonResponse["choices"][0]["finish_reason"].ToObject<string>() switch
				{
					"stop" => FinishReason.Stop,
					"length" => FinishReason.Length,
					"content_filter" => FinishReason.ContentFilter,
					"function_call" => FinishReason.FunctionCall,
					"tool_calls" => FinishReason.FunctionCall,
					_ => throw new Exception($"Unknown finish reason: {jsonResponse["choices"][0]["finish_reason"]}"),
				};

				return new ResponseEx
				{
					content = jsonResponse["choices"][0]["message"]["content"]?.ToString() ?? string.Empty,
					thinking = jsonResponse["choices"][0]["message"]["reasoning_content"]?.ToString(),
					FinishReason = finishReason,
					toolCallings = toolCalls,
				};
			}
			else
			{
				throw new Exception($"Error calling OpenAI API: {responseString}");
			}
		}

		public async IAsyncEnumerable<ResponseEx> GenerateChatExStream(PromptBuilder pb, CancellationToken cancellationToken)
		{
			var targetURL = $"{url}/chat/completions";
			var messages = BuildMessages(pb.Messages);
			var requestBody = new
			{
				model,
				messages,
				temperature,
				max_tokens,
				tools = pb.AvailableTools != null ? BuildTools(pb.AvailableTools) : null,
				stream = true
			};

			using var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

			var jsonContent = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
			var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

			using var request = new HttpRequestMessage(HttpMethod.Post, targetURL) { Content = content };
			using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			response.EnsureSuccessStatusCode();

			using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
			using var reader = new StreamReader(responseStream, Encoding.UTF8);


			while (await reader.ReadLineAsync(cancellationToken) is string line)
			{
				if (string.IsNullOrWhiteSpace(line))
					continue;

				// Handle SSE data lines
				var match = Regex.Match(line, @"^data: (.*)$");
				if (!match.Success)
					continue;

				var json = match.Groups[1].Value;

				// Check for [DONE] message
				if (json == "[DONE]")
					break;

				JObject data;
				try
				{
					data = JObject.Parse(json);
				}
				catch (JsonException)
				{
					continue;
				}

				if (data["choices"]?[0] == null)
					continue;

				var choice = data["choices"][0];
				var delta = choice["delta"];

				// Initialize empty response
				ResponseEx responseEx = new ResponseEx { content = "", FinishReason = FinishReason.None };

				responseEx.content = delta?["content"]?.ToString() ?? "";


				// Process tool calls
				if (delta?["tool_calls"] != null)
				{
					var toolCalls = new List<ToolCall>();
					var toolCallsArray = delta["tool_calls"] as JArray;
					if (toolCallsArray != null)
					{
						foreach (var item in toolCallsArray)
						{
							int index = item["index"]?.ToObject<int>() ?? 0;
							ToolCall existingCall = toolCalls.FirstOrDefault(t => t.index == index);

							// New tool call
							var newCall = new ToolCall
							{
								id = item["id"]?.ToString() ?? string.Empty,
								name = item["function"]?["name"]?.ToString() ?? string.Empty,
								arguments = item["function"]?["arguments"]?.ToString() ?? string.Empty,
								index = index
							};
							toolCalls.Add(newCall);
						}
					}

					responseEx.toolCallings = toolCalls;
				}
				else
				{
					responseEx.toolCallings = [];
				}

				// Process Finish Reason
				string? finishReasonString = choice?["finish_reason"]?.ToString();
				var finishReason = finishReasonString switch
				{
					"stop" => FinishReason.Stop,
					"length" => FinishReason.Length,
					"content_filter" => FinishReason.ContentFilter,
					"function_call" => FinishReason.FunctionCall,
					"tool_calls" => FinishReason.FunctionCall,
					_ => FinishReason.None
				};
				responseEx.FinishReason = finishReason;

				// Final response 
				yield return responseEx;
			}
		}

		public async Task<string> GenerateChatReply(PromptBuilder promptBuilder)
		{
			var targetURL = $"{url}/chat/completions";
			var messages = promptBuilder.Messages.Select(m => new { role = m.Item2.ToString(), content = m.Item1.Content }).ToArray();
			var requestBody = new
			{
				model,
				messages,
				temperature,
				max_tokens,
				stream = false
			};
			using (var client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
				var jsonContent = JsonConvert.SerializeObject(requestBody);
				var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
				var response = await client.PostAsync(targetURL, content);
				var responseString = await response.Content.ReadAsStringAsync();
				if (response.IsSuccessStatusCode)
				{
					JObject jsonResponse = JObject.Parse(responseString);
					return jsonResponse["choices"][0]["message"]["content"].ToString();
				}
				else
				{
					throw new Exception($"Error calling OpenAI API: {responseString}");
				}
			}
		}

		public async IAsyncEnumerable<string> GenerateChatReplyStream(PromptBuilder promptBuilder, CancellationToken cancellationToken)
		{
			var targetURL = $"{url}/chat/completions";
			var messages = promptBuilder.Messages.Select(m => new { role = m.Item2.ToString(), content = m.Item1.Content }).ToArray();

			using (var client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

				var request = new HttpRequestMessage(HttpMethod.Post, targetURL)
				{
					Content = new StringContent(JsonConvert.SerializeObject(new
					{
						model = model,
						messages = messages,
						stream = true
					}), Encoding.UTF8, "application/json")
				};

				using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None))
				{
					if (!response.IsSuccessStatusCode)
					{
						throw new Exception($"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
					}

					// Read the response body as a stream.
					using (var responseStream = await response.Content.ReadAsStreamAsync())
					using (var reader = new StreamReader(responseStream, Encoding.UTF8))
					{
						string? line;
						while ((line = await reader.ReadLineAsync()) != null)
						{
							if (string.IsNullOrWhiteSpace(line)) continue;

							// Handle SSE data lines, which look like: "data: {\"choices\":[{\"index\":0,\"delta\":{\"content\":\"text\"},\"finish_reason\":null}],\"object\":\"chat.completion.chunk\",\"created\":1677652943,\"model\":\"gpt-3.5-turbo-0613\"}"
							var match = Regex.Match(line, @"^data: (.*)$");
							if (match.Success)
							{
								var json = match.Groups[1].Value;

								dynamic? data = 1;
								try
								{
									data = JsonConvert.DeserializeObject(json);
								}
								catch
								{
									if (json == "[DONE]") break;
									else throw;
								}

								if (data != null)
									foreach (var choice in data.choices)
									{
										if (choice.delta != null && choice.delta.content != null)
										{
											yield return choice.delta.content;
										}
									}
							}
						}
					}
				}
			}
		}

		[Obsolete("对于新版本的OpenAI模型，这个接口无效，仅用于使用老版本OpenAI接口的API")]
		public async Task<string> GenerateText(string prompt, int retry = 0)
		{
			var uri = new Uri(url + "/completions");
			var requestBody = new
			{
				model,
				prompt,
				max_tokens,
				temperature
			};

			HttpClient client = new();
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

			var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
			var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

			var response = client.PostAsync(uri, content).Result;
			var responseString = response.Content.ReadAsStringAsync().Result;

			if (response.IsSuccessStatusCode)
			{
				JObject jsonResponse = JObject.Parse(responseString);
				return jsonResponse["choices"][0]["text"].ToString();
			}
			else
			{
				throw new Exception($"Error calling OpenAI API: {responseString}");
			}
		}

		IAsyncEnumerable<string> ILLMAPI.GenerateTextStream(string prompt, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region extra api

		public async Task<List<string>> GetModelNameList()
		{
			var targetURL = $"{url}/models";
			using var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
			var response = await client.GetAsync(targetURL);
			var responseString = await response.Content.ReadAsStringAsync();
			if (response.IsSuccessStatusCode)
			{
				JObject jsonResponse = JObject.Parse(responseString);
				var modelNames = jsonResponse["data"].Select(x => x["id"].ToString()).ToList();
				return modelNames;
			}
			else
			{
				throw new Exception($"Error calling OpenAI API: {responseString}");
			}
		}

		public void ChangeModel(string newModelName)
		{
			model = newModelName;
		}

		#endregion
		List<dynamic> BuildMessages(IEnumerable<(ChatMessage, PromptBuilder.From)> messages)
		{
			List<dynamic> dynamics = new();
			foreach (var message in messages)
			{
				if (message.Item2 == PromptBuilder.From.tool_call)
				{
					dynamics.Add(new
					{
						role = "assistant",
						content = "",
						tool_calls = message.Item1.toolCalls.Select(x => new
						{
							index = x.index,
							id = x.id,
							type = "function",
							function = new
							{
								name = x.name,
								arguments = x.arguments
							}
						})
					});
				}
				else
				if (message.Item2 == PromptBuilder.From.tool_result)
				{
					dynamics.Add(new
					{
						role = "tool",
						tool_call_id = message.Item1.id,
						content = message.Item1.Content
					});
				}
				else
				{
					if (message.Item1.ImageBase64 != null)
					{
						dynamics.Add(new
						{
							role = message.Item2 switch
							{
								PromptBuilder.From.system => "system",
								PromptBuilder.From.user => "user",
								PromptBuilder.From.assistant => "assistant",
							},
							input = new dynamic[]
							{
							new
							{
								type = "input_text",
								text = message.Item1.Content
							},
							new
							{
								type = "input_image",
								image_url = $"data:image/jpeg;base64,{message.Item1.ImageBase64}"
							}
							}
						});
					}
					else
					{
						dynamics.Add(new
						{
							role = message.Item2 switch
							{
								PromptBuilder.From.system => "system",
								PromptBuilder.From.user => "user",
								PromptBuilder.From.assistant => "assistant",
							},
							content = message.Item1.Content
						});
					}
				}
			}
			return dynamics;
		}
		List<OTool> BuildTools(IEnumerable<Tool> tools)
		{
			List<OTool> otools = tools.Select(
				x => new OTool
				{
					type = "function",
					function = new()
					{
						name = x.name,
						description = x.description,
						parameters = new()
						{
							type = "object",
							properties = x.parameters.ToDictionary(
								x => x.parameter.name,
								x => new OToolFunctionParameterProperty
								{
									type = x.parameter.type.ToString().ToLower(),
									description = x.parameter.description,
									@enum = x.parameter.@enum
								}
							),
							required = x.parameters.Where(x => x.required).Select(x => x.parameter.name).ToList()
						}
					}
				}
			).ToList();
			return otools;
		}

		struct OTool
		{
			public string type { get; set; }
			public OToolFunction function { get; set; }
		}

		struct OToolFunction
		{
			public string name { get; set; }
			public string description { get; set; }
			public OToolFunctionParameter parameters { get; set; }
		}

		struct OToolFunctionParameter
		{
			public string type { get; set; }
			public Dictionary<string, OToolFunctionParameterProperty> properties { get; set; }
			public List<string> required { get; set; }
		}
		struct OToolFunctionParameterProperty
		{
			public string type { get; set; }
			public string description { get; set; }
			[JsonProperty("enum")]
			public List<string> @enum { get; set; }
		}
	}
}
