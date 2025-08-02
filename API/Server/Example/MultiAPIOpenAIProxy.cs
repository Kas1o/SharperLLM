using SharperLLM.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.API.Server.Example
{
	public class MultiAPIOpenAIProxy
	{
		public List<LLMInfo> api;
		private OpenAIServer server;

		// 用于跟踪每个API的当前并发请求数
		private readonly Dictionary<ILLMAPI, int> apiConcurrency = new Dictionary<ILLMAPI, int>();
		private readonly Random random = new Random();

		public MultiAPIOpenAIProxy()
		{
			server = new OpenAIServer(
				handler: HandleRequest,
				handlerStream: HandleRequestStream,
				port: 34537,
				models: new List<string> { "gpt-3.5-turbo", "gpt-4" },
				allowRequestAnyModel: true
			);
		}

		public void Start()
		{
			if (api == null)
			{
				throw new InvalidOperationException("cannot start server when api not set.");
			}

			// 初始化并发计数器
			foreach (var llmInfo in api)
			{
				apiConcurrency[llmInfo.lLMAPI] = 0;
			}

			server.Start();
			Console.WriteLine("Listening... Press Ctrl+C to exit.");
		}

		private async Task<string> HandleRequest((PromptBuilder pb, string model) data)
		{
			var sb = new StringBuilder();
			await foreach (var chunk in HandleRequestStream(data))
			{
				sb.Append(chunk);
			}
			return sb.ToString();
		}

		private async IAsyncEnumerable<string> HandleRequestStream((PromptBuilder pb, string model) data)
		{
			// 1. 只使用未满负荷的API
			// 2. 优先使用有 affinity 的模型
			// 3. 否则随机调用
			// 4. 如果没有任何一个API可用，则等待

			while (true)
			{
				// 获取所有可用的API（未满负荷的）
				var availableApis = GetAvailableApis(data.model);

				if (availableApis.Count > 0)
				{
					// 2. 优先使用有 affinity 的模型
					var affinityApis = availableApis.Where(api =>
						api.modelAffinity != null && api.modelAffinity.Contains(data.model)).ToList();

					LLMInfo selectedApi;
					if (affinityApis.Count > 0)
					{
						// 如果有affinity的API，随机选择一个
						selectedApi = affinityApis[random.Next(affinityApis.Count)];
					}
					else
					{
						// 3. 否则随机调用
						selectedApi = availableApis[random.Next(availableApis.Count)];
					}

					// 增加并发计数
					apiConcurrency[selectedApi.lLMAPI]++;

					try
					{
						// 调用选中的API
						var stream = selectedApi.useTextCompletion
							? selectedApi.lLMAPI.GenerateTextStream
							(
								new PromptBuilder(selectedApi.promptBuilderTemplate)
								{
									Messages = data.pb.Messages
								}.GeneratePromptWithLatestOuputPrefix()
							)
							: selectedApi.lLMAPI.GenerateChatReplyStream(data.pb);

						await foreach (var chunk in stream)
						{
							yield return chunk;
						}

						yield break; // 成功完成，退出循环
					}
					finally
					{
						// 减少并发计数
						apiConcurrency[selectedApi.lLMAPI]--;
					}
				}
				else
				{
					// 4. 如果没有任何一个API可用，则等待
					await Task.Delay(1000); // 等待1秒后重试
				}
			}
		}

		private List<LLMInfo> GetAvailableApis(string model)
		{
			return api.Where(apiInfo =>
			{
				// 检查API是否支持该模型
				if (apiInfo.modelAffinity != null && !apiInfo.modelAffinity.Contains(model))
				{
					return false;
				}

				// 检查是否未满负荷
				return apiConcurrency[apiInfo.lLMAPI] < apiInfo.concurrency;
			}).ToList();
		}
	}

	public struct LLMInfo
	{
		required public ILLMAPI lLMAPI;
		required public int concurrency;
		required public bool useTextCompletion;
		required public PromptBuilder promptBuilderTemplate;
		required public string[] modelAffinity;
	}
}
