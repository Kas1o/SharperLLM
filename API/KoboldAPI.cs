using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharperLLM.FunctionCalling;
using SharperLLM.Util;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SharperLLM.API
{
	public class KoboldAPI : ILLMAPI
	{
		public class KoboldAPIConf
		{
			public int? max_context_length { get; set; } = 8192;
			public int? max_length { get; set; } = 1024;
			public float? rep_pen { get; set; } = 1.15f;
			public int? rep_pen_range { get; set; } = 512;
			public int? sampler_seed { get; set; }
			public string[] stop_sequence { get; set; } = new string[] { "</s>", "<|im_end|>" };

			public float? temperature { get; set; } = 0.7f;
			public float? tfs { get; set; } //无尾
			public float? top_a { get; set; } = 0;
			public int? top_k { get; set; } = 20;
			public float? top_p { get; set; }
			public float? min_p { get; set; }
			public float? typical { get; set; } = 1; //典型
			public float? dynatemp_range { get; set; } = 0;
			public float? smoothing_factor { get; set; } = 0;
			public float? dynatemp_exponent { get; set; } = 1;
			public int? mirostat { get; set; } = 0;
			public int? mirostat_tau { get; set; } = 5;
			public float? mirostat_eta { get; set; } = 0.46f;
			public string genkey { get; set; }
			public string grammar { get; set; }
			public string images { get; set; } //base64
			public bool? trim_stop { get; set; }
			public bool? bypass_eos { get; set; }
			public string[] banned_tokens { get; set; }
			public Dictionary<int, float> logit_bias { get; set; } // token id => bias.
			public string prompt { get; set; } // do not change
		}

		public KoboldAPIConf conf = new();
		public Uri _uri;
		public string uri
		{
			get => _uri.AbsoluteUri;
			set
			{
				//validate
				_uri = CreateLooseUri(value);
			}
		}

		public KoboldAPI(string uri, KoboldAPIConf conf)
		{
			this.conf = conf;
			_uri = CreateLooseUri(uri);
		}
		public KoboldAPI(string uri)
		{
			_uri = CreateLooseUri(uri);
		}
		private static Uri CreateLooseUri(string looseUriString)
		{
			looseUriString = looseUriString.TrimEnd('/');
			if (string.IsNullOrWhiteSpace(looseUriString))
			{
				throw new ArgumentException("URI string cannot be null, empty, or whitespace.", nameof(looseUriString));
			}

			// 检查是否已包含http://或https://，如果没有则添加http://
			if (!looseUriString.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
				!looseUriString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				looseUriString = "http://" + looseUriString;
			}

			Uri uri;
			if (Uri.TryCreate(looseUriString, UriKind.Absolute, out uri))
			{
				return uri;
			}
			else
			{
				throw new ArgumentException($"The provided string '{looseUriString}' could not be parsed into a valid URI.");
			}
		}

		public async IAsyncEnumerable<string> GenerateTextStream(string prompt)
		{
			conf.prompt = prompt;
			var client = new HttpClient();

			client.BaseAddress = new Uri(uri + "/extra/generate/stream");

			var request = new HttpRequestMessage(HttpMethod.Post, uri + "/extra/generate/stream")
			{
				Content = new StringContent(JsonConvert.SerializeObject(conf, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
				Encoding.UTF8, "application/json")
			};

			using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

			await using var stream = await response.Content.ReadAsStreamAsync();

			_ = response.Content.Headers.TryGetValues("Content-Type", out var contentTypes);

			if (contentTypes.Any(x => x.StartsWith("text/event-stream")))
			{
				using var reader = new StreamReader(stream);

				while (!reader.EndOfStream)
				{
					var line = await reader.ReadLineAsync();

					if (line.StartsWith("data:"))
					{
						var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(line.Substring(5));
						yield return data["token"];
					}
				}
			}
		}
		public async Task<string> GenerateText(string prompt, int retry = 0)
		{
			conf.prompt = prompt;
			var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(999) };
			client.BaseAddress = new Uri(uri + "/v1/generate");

			var request = new HttpRequestMessage(HttpMethod.Post, uri + "/v1/generate")
			{
				Content = new StringContent(JsonConvert.SerializeObject(conf, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
											Encoding.UTF8, "application/json")
			};


			try
			{
				// 发送请求并获取响应
				var response = client.Send(request);
				response.EnsureSuccessStatusCode();

				// 读取响应内容
				string responseBody = response.Content.ReadAsStringAsync().Result;

				// 解析JSON响应，提取"text"字段  
				JObject json = JObject.Parse(responseBody);
				JArray results = (JArray)json["results"];
				if (results.Count > 0)
				{
					string text = results[0]["text"].ToString();
					return text; // 直接返回提取的文本内容
				}
				else
				{
					throw new ApplicationException("No 'results' found in the response.");
				}
			}
			catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
			{
				// 将503服务繁忙异常直接抛出
				throw;
			}
			catch (Exception ex)
			{
				if (retry > 0)
				{
					return await GenerateText(prompt, retry - 1);
				}

				// 将其他异常也直接抛出
				throw new ApplicationException($"An error occurred while processing the request:{ex.Message}", ex);
			}
		}

		public IAsyncEnumerable<string> GenerateChatReplyStream(PromptBuilder promptBuilder)
		{
			throw new NotImplementedException();
		}

		public Task<string> GenerateChatReply(PromptBuilder promptBuilder)
		{
			throw new NotImplementedException();
		}

		public async Task<ResponseEx> GenerateEx(PromptBuilder pb)
		{
			int retry = 5;
			while (retry > 0) {
				try
				{
					var result = await GenerateText(pb.GeneratePromptWithLatestOuputPrefix());
					// 如果调用了工具，则处理。
					if (result.Contains(pb.ToolsCallPrefix))
					{
						List<Tool> toolCallings;
						StringBuilder contentBuilder;
						ProcessContentAndTool(pb, result, out toolCallings, out contentBuilder);
						if (toolCallings.Count < 1) throw new Exception();// 说明有调用工具，但格式错误。
						return new ResponseEx()
						{
							toolCallings = toolCallings,
							content = contentBuilder.ToString().Trim()
						};
					}
					else//如果没有调用工具，则添加工具前缀再进行生成
					{
						var cpb = pb.Clone();
						cpb.Messages = cpb.Messages.Append((result, PromptBuilder.From.assistant)).ToArray();
						var prompt = cpb.GenerateCleanPrompt() + cpb.ToolsCallPrefix;
						var result2 = await GenerateText(prompt);
						return new ResponseEx()
						{
							toolCallings = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Tool>>(result2),
							content = result
						};
					}
				}
				catch
				{
					retry--;
				}
			}
			return null;
		}

		private static void ProcessContentAndTool(PromptBuilder pb, string result, out List<Tool> toolCallings, out StringBuilder contentBuilder)
		{
			// 初始化输出参数
			toolCallings = new List<Tool>();
			contentBuilder = new System.Text.StringBuilder(result); // 初始时包含全部内容

			// 定义正则表达式模式，并启用单行模式（. 匹配包括换行符在内的所有字符）
			var toolPattern = new Regex($@"{Regex.Escape(pb.ToolsCallPrefix)}(.*?){Regex.Escape(pb.ToolsCallSuffix)}", RegexOptions.Singleline);

			// 查找所有匹配项
			var matches = toolPattern.Matches(result);

			// 反向遍历以避免删除过程中影响索引
			for (int i = matches.Count - 1; i >= 0; i--)
			{
				Match match = matches[i];
				try
				{
					// 尝试解析为Tool对象列表
					var tools = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Tool>>(match.Groups[1].Value);
					if (tools != null)
					{
						toolCallings.InsertRange(0, tools); // 在前面插入以保持原始顺序
															// 从contentBuilder中移除已处理的工具调用部分
						contentBuilder.Remove(match.Index, match.Length);
					}
				}
				catch
				{
					// 解析失败时跳过该块
					continue;
				}
			}

			// 移除可能因删除操作导致的多余空白
			contentBuilder.Replace("  ", " ");
		}

		public IAsyncEnumerable<ResponseEx> GenerateExStream(PromptBuilder pb)
		{
			throw new NotImplementedException();
		}
	}
}