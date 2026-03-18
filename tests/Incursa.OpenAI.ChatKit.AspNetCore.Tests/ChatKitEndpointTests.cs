using System.Text;
using Incursa.OpenAI.ChatKit.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Incursa.OpenAI.ChatKit.AspNetCore.Tests;

[Trait("Category", "Smoke")]
public sealed class ChatKitEndpointTests
{
    /// <summary>The ASP.NET Core endpoint adapter writes JSON responses for non-streaming ChatKit operations.</summary>
    /// <intent>Protect the HTTP boundary for the translated ChatKit server surface.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-002</scenario>
    /// <behavior>Posting a JSON ChatKit request returns a JSON payload with the expected thread data.</behavior>
    [Fact]
    public async Task MapChatKit_WritesJsonResponse()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<TestServerHandler>();

        WebApplication app = builder.Build();
        app.MapChatKit<TestServerHandler, Dictionary<string, object?>>("/chatkit", _ => new Dictionary<string, object?>());
        await app.StartAsync();

        try
        {
            HttpClient client = app.GetTestClient();
            string payload = Encoding.UTF8.GetString(ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsGetByIdRequest
            {
                Params = new ThreadGetByIdParams
                {
                    ThreadId = "thr_1",
                },
            }));

            HttpResponseMessage response = await client.PostAsync("/chatkit", new StringContent(payload, Encoding.UTF8, "application/json"));
            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("\"id\":\"thr_1\"", body, StringComparison.Ordinal);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private sealed class TestServerHandler : ChatKitServer<Dictionary<string, object?>>
    {
        public TestServerHandler()
            : base(CreateStore())
        {
        }

        public override async IAsyncEnumerable<ThreadStreamEvent> RespondAsync(
            ThreadMetadata thread,
            UserMessageItem? inputUserMessage,
            Dictionary<string, object?> context,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new ThreadItemDoneEvent
            {
                Item = new AssistantMessageItem
                {
                    Id = "msg_1",
                    ThreadId = thread.Id,
                    CreatedAt = ChatKitClock.Now(),
                    Content = [new AssistantMessageContent { Text = "hello" }],
                },
            };

            await Task.CompletedTask;
        }

        private static InMemoryChatKitStore<Dictionary<string, object?>> CreateStore()
        {
            InMemoryChatKitStore<Dictionary<string, object?>> store = new();
            store.SaveThreadAsync(new ThreadMetadata
            {
                Id = "thr_1",
                CreatedAt = ChatKitClock.Now(),
                Title = "seeded",
            }, new Dictionary<string, object?>()).GetAwaiter().GetResult();
            return store;
        }
    }
}
