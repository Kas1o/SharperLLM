using SharperLLM.API;
using SharperLLM.FunctionCalling;
using SharperLLM.Util;

namespace SharperLLM.Agents;

public sealed class ToolCallLoopRunner
{
    public async Task RunAsync(
        IChatCompletionClient llmApi,
        IPromptContext promptContext,
        IReadOnlyList<Tool> toolDefinitions,
        IToolExecutor toolExecutor,
        ToolCallLoopOptions? options = null,
        ToolCallLoopCallbacks? callbacks = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ToolCallLoopOptions();

        if (options.MaxRounds <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.MaxRounds), "MaxRounds must be greater than zero.");

        for (var round = 1; round <= options.MaxRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            callbacks?.OnRoundStart?.Invoke(round);

            var promptBuilder = promptContext.BuildPromptBuilder(toolDefinitions);
            var response = await llmApi.GenerateAsync(promptBuilder);

            callbacks?.OnRoundCompleted?.Invoke(round, response);
            promptContext.AppendAssistantMessage(CloneChatMessage(response.Body));

            if (response.FinishReason != FinishReason.FunctionCall || response.Body.toolCalls == null || response.Body.toolCalls.Count == 0)
                return;

            foreach (var toolCall in response.Body.toolCalls)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var toolResult = await ExecuteToolCallAsync(toolExecutor, toolCall, options, cancellationToken);

                promptContext.AppendToolResult(toolCall.id, toolCall.name, toolResult);
                callbacks?.OnToolResult?.Invoke(round, toolCall, toolResult);
            }
        }

        if (options.ThrowOnRoundLimitReached)
            throw new InvalidOperationException($"Tool call loop exceeded max rounds ({options.MaxRounds}).");
    }

    public async Task RunStreamAsync(
        IChatCompletionClient llmApi,
        IPromptContext promptContext,
        IReadOnlyList<Tool> toolDefinitions,
        IToolExecutor toolExecutor,
        ToolCallLoopOptions? options = null,
        ToolCallLoopStreamCallbacks? callbacks = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ToolCallLoopOptions();

        if (options.MaxRounds <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.MaxRounds), "MaxRounds must be greater than zero.");

        for (var round = 1; round <= options.MaxRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            callbacks?.OnRoundStart?.Invoke(round);

            var promptBuilder = promptContext.BuildPromptBuilder(toolDefinitions);
            var stream = llmApi.GenerateStreamAsync(promptBuilder, cancellationToken);

            var accumulatedResponse = new ResponseEx
            {
                Body = new ChatMessage { Content = string.Empty },
                FinishReason = FinishReason.None
            };

            await foreach (var chunk in stream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                callbacks?.OnRoundDelta?.Invoke(round, chunk);
                accumulatedResponse += chunk;
            }

            callbacks?.OnRoundCompleted?.Invoke(round, accumulatedResponse);
            promptContext.AppendAssistantMessage(CloneChatMessage(accumulatedResponse.Body));

            if (accumulatedResponse.FinishReason != FinishReason.FunctionCall ||
                accumulatedResponse.Body.toolCalls == null ||
                accumulatedResponse.Body.toolCalls.Count == 0)
            {
                return;
            }

            foreach (var toolCall in accumulatedResponse.Body.toolCalls)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var toolResult = await ExecuteToolCallAsync(toolExecutor, toolCall, options, cancellationToken);
                promptContext.AppendToolResult(toolCall.id, toolCall.name, toolResult);
                callbacks?.OnToolResult?.Invoke(round, toolCall, toolResult);
            }
        }

        if (options.ThrowOnRoundLimitReached)
            throw new InvalidOperationException($"Tool call loop exceeded max rounds ({options.MaxRounds}).");
    }

    private static async Task<string> ExecuteToolCallAsync(
        IToolExecutor toolExecutor,
        ToolCall toolCall,
        ToolCallLoopOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var rawResult = await toolExecutor.ExecuteAsync(toolCall, cancellationToken);
            return string.IsNullOrWhiteSpace(rawResult)
                ? options.EmptyToolResultPlaceholder
                : rawResult;
        }
        catch (Exception ex)
        {
            if (!options.ContinueOnToolError)
                throw;

            return $"[Tool error] {ex.Message}";
        }
    }

    private static ChatMessage CloneChatMessage(ChatMessage original)
    {
        return new ChatMessage
        {
            Content = original.Content,
            ImageBase64 = original.ImageBase64,
            thinking = original.thinking,
            id = original.id,
            toolCalls = original.toolCalls?.Select(tc => new ToolCall
            {
                name = tc.name,
                id = tc.id,
                arguments = tc.arguments,
                index = tc.index
            }).ToList(),
            CustomProperties = original.CustomProperties != null
                ? new Dictionary<string, object>(original.CustomProperties)
                : null
        };
    }
}
