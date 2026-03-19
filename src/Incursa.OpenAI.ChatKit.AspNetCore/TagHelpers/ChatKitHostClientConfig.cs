namespace Incursa.OpenAI.ChatKit.AspNetCore.TagHelpers;

internal sealed class ChatKitHostClientConfig
{
    public string? ApiUrl { get; init; }

    public string? DomainKey { get; init; }

    public string? SessionEndpoint { get; init; }

    public string? ActionEndpoint { get; init; }

    public string? Height { get; init; }

    public string? Locale { get; init; }

    public string? FrameTitle { get; init; }

    public string? InitialThread { get; init; }

    public string? ClientToolHandlers { get; init; }

    public string? EntityHandlers { get; init; }

    public string? WidgetActionHandler { get; init; }

    public ChatKitThemeClientConfig? Theme { get; init; }

    public ChatKitHeaderClientConfig? Header { get; init; }

    public ChatKitHistoryClientConfig? History { get; init; }

    public ChatKitStartScreenClientConfig? StartScreen { get; init; }

    public ChatKitComposerClientConfig? Composer { get; init; }

    public ChatKitDisclaimerClientConfig? Disclaimer { get; init; }

    public ChatKitEntitiesClientConfig? Entities { get; init; }

    public ChatKitThreadItemActionsClientConfig? ThreadItemActions { get; init; }

    public ChatKitWidgetActionsClientConfig? WidgetActions { get; init; }
}

internal sealed class ChatKitThemeClientConfig
{
    public string? ColorScheme { get; init; }

    public string? Radius { get; init; }

    public string? Density { get; init; }
}

internal sealed class ChatKitHeaderClientConfig
{
    public bool? Enabled { get; init; }

    public ChatKitHeaderTitleClientConfig? Title { get; init; }
}

internal sealed class ChatKitHeaderTitleClientConfig
{
    public string? Text { get; init; }
}

internal sealed class ChatKitHistoryClientConfig
{
    public bool? Enabled { get; init; }

    public bool? ShowDelete { get; init; }

    public bool? ShowRename { get; init; }
}

internal sealed class ChatKitStartScreenClientConfig
{
    public string? Greeting { get; init; }

    public IReadOnlyList<ChatKitStartPromptClientConfig>? Prompts { get; init; }
}

internal sealed class ChatKitStartPromptClientConfig
{
    public required string Label { get; init; }

    public required string Prompt { get; init; }

    public string? Icon { get; init; }
}

internal sealed class ChatKitComposerClientConfig
{
    public string? Placeholder { get; init; }
}

internal sealed class ChatKitDisclaimerClientConfig
{
    public string? Text { get; init; }

    public bool? HighContrast { get; init; }
}

internal sealed class ChatKitEntitiesClientConfig
{
    public bool? ShowComposerMenu { get; init; }
}

internal sealed class ChatKitThreadItemActionsClientConfig
{
    public bool? Feedback { get; init; }

    public bool? Retry { get; init; }
}

internal sealed class ChatKitWidgetActionsClientConfig
{
    public bool ForwardToEndpoint { get; init; }
}
