using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Incursa.OpenAI.ChatKit.AspNetCore;

public static class ChatKitEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapChatKit<TServer, TContext>(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<HttpContext, TContext> contextFactory)
        where TServer : ChatKitServer<TContext>
    {
        return endpoints.MapPost(pattern, async (HttpContext httpContext, TServer server, CancellationToken cancellationToken) =>
        {
            using MemoryStream buffer = new();
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
