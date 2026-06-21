using SharperLLM.API;

namespace SharperLLM.Agents;

public sealed class ToolCallLoopOptions
{
    public int MaxRounds { get; set; } = 16;
    public bool ContinueOnToolError { get; set; } = true;
    public string EmptyToolResultPlaceholder { get; set; } = "[No result]";
    public bool ThrowOnRoundLimitReached { get; set; } = true;
}

public sealed class ToolCallLoopCallbacks
{
    /// <summary>
    /// Called when a round starts.
    /// Useful for round-level logging, telemetry, and UI state transitions.
    /// </summary>
    public Action<int>? OnRoundStart { get; set; }

    /// <summary>
    /// Called when a non-stream round completes with the full model response.
    /// Use this as the primary place to read assistant output in non-stream mode.
    /// </summary>
    public Action<int, ResponseEx>? OnRoundCompleted { get; set; }

    /// <summary>
    /// Called after each tool call is executed.
    /// Useful for tool transcript and execution diagnostics.
    /// </summary>
    public Action<int, ToolCall, string>? OnToolResult { get; set; }
}

public sealed class ToolCallLoopStreamCallbacks
{
    public Action<int>? OnRoundStart { get; set; }

    /// <summary>
    /// Stream delta callback.
    ///
    /// This callback receives raw incremental chunks from the model. Use this for
    /// live rendering and append only the current delta content to UI/output.
    /// Do not re-concatenate previous chunks here, or text will be duplicated.
    /// </summary>
    public Action<int, ResponseEx>? OnRoundDelta { get; set; }

    /// <summary>
    /// Round completed callback with fully accumulated response.
    ///
    /// Use this for persistence, analytics, and post-processing. Do not append
    /// this content again to a surface that already consumed OnRoundDelta.
    /// </summary>
    public Action<int, ResponseEx>? OnRoundCompleted { get; set; }

    /// <summary>
    /// Tool execution result callback.
    ///
    /// Use this for tool logs, observability, and tool-result transcript display.
    /// </summary>
    public Action<int, ToolCall, string>? OnToolResult { get; set; }
}
