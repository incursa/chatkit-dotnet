namespace Incursa.OpenAI.ChatKit.Benchmarks;

internal static class ChatKitBenchmarkData
{
    public static readonly ThreadsCreateRequest ThreadsCreateRequest = new()
    {
        Params = new ThreadCreateParams
        {
            Input = new UserMessageInput
            {
                Content =
                [
                    new UserMessageTextContent { Text = "Summarize the latest workspace changes." },
                    new UserMessageTextContent { Text = "Call out anything risky." },
                ],
            },
        },
    };

    public static readonly ThreadsGetByIdRequest ThreadsGetByIdRequest = new()
    {
        Params = new ThreadGetByIdParams
        {
            ThreadId = "thr_benchmark",
        },
    };

    public static readonly WidgetRoot BeforeStreamingWidget = new()
    {
        Type = "Box",
        Children =
        [
            new WidgetComponent
            {
                Type = "Text",
                Id = "summary",
                Properties = new Dictionary<string, object?>
                {
                    ["value"] = "Hel",
                    ["streaming"] = true,
                },
            },
        ],
    };

    public static readonly WidgetRoot AfterStreamingWidget = new()
    {
        Type = "Box",
        Children =
        [
            new WidgetComponent
            {
                Type = "Text",
                Id = "summary",
                Properties = new Dictionary<string, object?>
                {
                    ["value"] = "Hello",
                    ["streaming"] = false,
                },
            },
        ],
    };

    public static readonly WidgetRoot FullReplaceWidget = new()
    {
        Type = "Card",
        Children =
        [
            new WidgetComponent
            {
                Type = "Text",
                Id = "summary",
                Properties = new Dictionary<string, object?>
                {
                    ["value"] = "Hello",
                    ["streaming"] = false,
                },
            },
        ],
    };
}
