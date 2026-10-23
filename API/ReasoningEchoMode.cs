namespace SharperLLM.API
{
	/// <summary>
	/// 请求中如何回传 assistant 消息的推理内容（<c>reasoning_content</c>）。
	/// </summary>
	public enum ReasoningEchoMode
	{
		/// <summary>
		/// 完整回传：所有带 thinking 的 assistant 消息都附加 <c>reasoning_content</c>。
		/// </summary>
		Full = 0,

		/// <summary>
		/// 仅回传最后一条 user 消息之后的 assistant 消息（当前生成链，通常是一轮工具调用）。
		/// </summary>
		AfterLastUser = 1,

		/// <summary>
		/// 不回传：请求中不携带 <c>reasoning_content</c>。
		/// </summary>
		None = 2
	}
}
