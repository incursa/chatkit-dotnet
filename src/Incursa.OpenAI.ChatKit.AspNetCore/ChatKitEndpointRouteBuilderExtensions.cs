using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Incursa.OpenAI.ChatKit.AspNetCore;

/// <summary>
/// Provides ASP.NET Core endpoint mapping helpers for ChatKit servers.
/// </summary>
public static class ChatKitEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a POST endpoint that forwards ChatKit protocol requests to a <see cref="ChatKitServer{TContext}"/>.
    /// </summary>
    /// <typeparam name="TServer">The ChatKit server type resolved from dependency injection.</typeparam>
    /// <typeparam name="TContext">The request context type created for each HTTP call.</typeparam>
    /// <param name="endpoints">The route builder used to register the endpoint.</param>
    /// <param name="pattern">The route pattern that should receive ChatKit requests.</param>
    /// <param name="contextFactory">Creates the ChatKit request context from the current <see cref="HttpContext"/>.</param>
    /// <returns>The endpoint convention builder for additional endpoint customization.</returns>
    public static IEndpointConventionBuilder MapChatKit<TServer, TContext>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<HttpContext, TContext> contextFactory)
        where TServer : ChatKitServer<TContext>
    {
        return endpoints.MapPost(pattern, async (HttpContext httpContext, TServer server, CancellationToken cancellationToken) =>
        {
            using MemoryStream buffer = new();
            // Keep the HTTP adapter intentionally dumb: the core server owns request
            // parsing, operation routing, and result classification.
            await httpContext.Request.Body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

            ChatKitProcessResult result = await server.ProcessAsync(buffer.ToArray(), contextFactory(httpContext), cancellationToken).ConfigureAwait(false);
            switch (result)
            {
                case NonStreamingResult nonStreaming:
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.Body.WriteAsync(nonStreaming.Json, cancellationToken).ConfigureAwait(false);
                    break;
                case StreamingResult streaming:
                    httpContext.Response.ContentType = "text/event-stream";
                    // Flush each chunk so the browser sees incremental ChatKit events
                    // instead of waiting for the full assistant turn to complete.
                    await foreach (byte[] chunk in streaming.WithCancellation(cancellationToken).ConfigureAwait(false))
                    {
                        await httpContext.Response.Body.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
                        await httpContext.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                    break;
            }
        });
    }
}
