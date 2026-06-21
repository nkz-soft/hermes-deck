using HermesDeck.Api.Auth;
using HermesDeck.Api.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HermesDeck.Api.Conversations;

/// <summary>
/// Request body for <c>POST /conversations</c>. The title is optional; the owning identity is never
/// taken from the request and is always derived from the validated session.
/// </summary>
public sealed record CreateConversationRequest(string? Title);

/// <summary>
/// Conversation projection returned to clients, matching the OpenAPI <c>Conversation</c> schema.
/// </summary>
public sealed record ConversationResponse(
    string ConversationId,
    string? Title,
    string Status,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Maps the conversation endpoints. <c>GET /conversations</c> lists conversations visible to the
/// current identity; <c>POST /conversations</c> creates a new conversation owned by the current
/// identity. Both require an authenticated current identity and return a generic <c>401</c> when one
/// is absent, never leaking protected detail.
/// </summary>
public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/conversations", async (
            ICurrentIdentityAccessor currentIdentityAccessor,
            IConversationService conversationService,
            CancellationToken cancellationToken) =>
        {
            var current = currentIdentityAccessor.Current;
            if (current is null)
            {
                return Results.Unauthorized();
            }

            var conversations = await conversationService.ListAsync(current.IdentityId, cancellationToken);

            return Results.Ok(conversations.Select(ToResponse).ToList());
        });

        app.MapPost("/conversations", async (
            CreateConversationRequest? request,
            ICurrentIdentityAccessor currentIdentityAccessor,
            IConversationService conversationService,
            CancellationToken cancellationToken) =>
        {
            var current = currentIdentityAccessor.Current;
            if (current is null)
            {
                return Results.Unauthorized();
            }

            var conversation = await conversationService.CreateAsync(
                current.IdentityId,
                request?.Title,
                cancellationToken);

            var response = ToResponse(conversation);

            return Results.Created($"/conversations/{conversation.ConversationId}", response);
        });

        return app;
    }

    private static ConversationResponse ToResponse(Conversation conversation) =>
        new(
            conversation.ConversationId,
            conversation.Title,
            ToStatusString(conversation.Status),
            conversation.UpdatedAt);

    private static string ToStatusString(ConversationStatus status) => status switch
    {
        ConversationStatus.Active => "active",
        ConversationStatus.Archived => "archived",
        ConversationStatus.Closed => "closed",
        _ => status.ToString().ToLowerInvariant()
    };
}
