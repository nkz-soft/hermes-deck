using System.Text.Json;
using HermesDeck.Api.Domain;
using HermesDeck.Api.Events;
using HermesDeck.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HermesDeck.Api.Runs;

/// <summary>
/// The result of accepting a submitted message: the persisted user message id and the agent run id
/// that was kicked off for it.
/// </summary>
public sealed record SubmitMessageResult(string MessageId, string RunId);

/// <summary>
/// Orchestrates the message-to-run lifecycle: persisting a user message, creating and starting an
/// agent run, publishing the initial status event, and dispatching the agent runner as background
/// work. Also exposes ownership-scoped run lookup. Identity is always supplied by the caller from the
/// validated session and is never trusted from request input.
/// </summary>
public interface IRunOrchestrator
{
    /// <summary>
    /// Persists a user message in the given conversation, creates a running <see cref="AgentRun"/>,
    /// updates the conversation's last run, publishes a <c>run.status.changed</c> (running) event, and
    /// dispatches the <see cref="IAgentChatRunner"/> as fire-and-forget background work. Does not block
    /// on the runner. Returns the new message and run ids.
    /// </summary>
    Task<SubmitMessageResult> SubmitMessageAsync(
        string conversationId,
        string identityId,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the run with the given id, returning <c>null</c> when it does not exist or is not owned
    /// (transitively) by <paramref name="identityId"/>. Callers map a <c>null</c> result to a generic
    /// <c>404</c> so existence is never leaked.
    /// </summary>
    Task<AgentRun?> GetRunAsync(
        string runId,
        string identityId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// EF Core-backed <see cref="IRunOrchestrator"/>. Request-path persistence uses the request-scoped
/// <see cref="HermesDeckDbContext"/>; the background runner is given its own DI scope via
/// <see cref="IServiceScopeFactory"/> so it never shares the request's DbContext (which is disposed
/// once the 202 response completes).
/// </summary>
public sealed class RunOrchestrator : IRunOrchestrator
{
    private readonly HermesDeckDbContext _db;
    private readonly IRunEventPublisher _eventPublisher;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RunOrchestrator> _logger;

    public RunOrchestrator(
        HermesDeckDbContext db,
        IRunEventPublisher eventPublisher,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<RunOrchestrator> logger)
    {
        _db = db;
        _eventPublisher = eventPublisher;
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<SubmitMessageResult> SubmitMessageAsync(
        string conversationId,
        string identityId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var message = new Message
        {
            MessageId = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            SenderType = MessageSenderType.User,
            Content = content,
            CreatedAt = now,
            StreamState = MessageStreamState.None
        };

        var run = new AgentRun
        {
            RunId = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            Status = AgentRunStatus.Running,
            RequestedByIdentityId = identityId,
            StartedAt = now
        };

        _db.Messages.Add(message);
        _db.AgentRuns.Add(run);

        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId, cancellationToken);
        if (conversation is not null)
        {
            conversation.LastRunId = run.RunId;
            conversation.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(
            new RunEvent(
                run.RunId,
                conversationId,
                "run.status.changed",
                "Run is running",
                now,
                SerializeRunStatusPayload(run.RunId, "running")),
            cancellationToken);

        DispatchRunner(new AgentChatRunContext(
            run.RunId,
            conversationId,
            identityId,
            message.MessageId,
            content));

        return new SubmitMessageResult(message.MessageId, run.RunId);
    }

    public async Task<AgentRun?> GetRunAsync(
        string runId,
        string identityId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(runId) || string.IsNullOrEmpty(identityId))
        {
            return null;
        }

        return await _db.AgentRuns
            .Join(
                _db.Conversations,
                run => run.ConversationId,
                conversation => conversation.ConversationId,
                (run, conversation) => new { Run = run, conversation.IdentityId })
            .Where(x => x.Run.RunId == runId && x.IdentityId == identityId)
            .Select(x => x.Run)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Dispatches the agent runner on a background task with its own DI scope. The 202 response must
    /// not wait on it, so this method does not await the task. Any unobserved failure is logged and the
    /// run is marked failed; failures never propagate back to the request path.
    /// </summary>
    private void DispatchRunner(AgentChatRunContext context)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IAgentChatRunner>();
                await runner.RunAsync(context, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Agent chat runner failed for run {RunId} in conversation {ConversationId}; marking run failed.",
                    context.RunId,
                    context.ConversationId);

                await TryMarkRunFailedAsync(context, ex.Message);
            }
        });
    }

    private async Task TryMarkRunFailedAsync(AgentChatRunContext context, string failureReason)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HermesDeckDbContext>();
            var run = await db.AgentRuns.FirstOrDefaultAsync(r => r.RunId == context.RunId);
            if (run is null)
            {
                return;
            }

            run.Status = AgentRunStatus.Failed;
            run.FailureReason = failureReason;
            run.CompletedAt = _timeProvider.GetUtcNow();
            await db.SaveChangesAsync();

            var publisher = scope.ServiceProvider.GetRequiredService<IRunEventPublisher>();
            await publisher.PublishAsync(new RunEvent(
                run.RunId,
                context.ConversationId,
                "run.status.changed",
                "Run failed",
                run.CompletedAt.Value,
                SerializeRunStatusPayload(run.RunId, "failed")));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to mark run {RunId} as failed after a runner error.",
                context.RunId);
        }
    }

    private static string SerializeRunStatusPayload(string runId, string status) =>
        JsonSerializer.Serialize(new { runId, status });
}
