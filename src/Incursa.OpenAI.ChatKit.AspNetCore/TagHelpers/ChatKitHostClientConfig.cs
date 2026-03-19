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

    public ChatKitFileUploadStrategyClientConfig? UploadStrategy { get; init; }

    public ChatKitDisclaimerClientConfig? Disclaimer { get; init; }

    public ChatKitEntitiesClientConfig? Entities { get; init; }

    public ChatKitThreadItemActionsClientConfig? ThreadItemActions { get; init; }

    public ChatKitWidgetActionsClientConfig? WidgetActions { get; init; }
}

internal sealed class ChatKitThemeClientConfig
{
    public string? ColorScheme { get; init; }

    public ChatKitThemeTypographyClientConfig? Typography { get; init; }

    public ChatKitThemeColorClientConfig? Color { get; init; }

    public string? Radius { get; init; }

    public string? Density { get; init; }
}

internal sealed class ChatKitThemeTypographyClientConfig
{
    public int? BaseSize { get; init; }

    public IReadOnlyList<ChatKitFontSourceClientConfig>? FontSources { get; init; }

    public string? FontFamily { get; init; }

    public string? FontFamilyMono { get; init; }
}

internal sealed class ChatKitFontSourceClientConfig
{
    public string? Family { get; init; }

    public string? Src { get; init; }

    public object? Weight { get; init; }

    public string? Style { get; init; }

    public string? Display { get; init; }

    public string? UnicodeRange { get; init; }
}

internal sealed class ChatKitThemeColorClientConfig
{
    public ChatKitThemeGrayscaleClientConfig? Grayscale { get; init; }

    public ChatKitThemeAccentColorClientConfig? Accent { get; init; }

    public ChatKitThemeSurfaceColorsClientConfig? Surface { get; init; }
}

internal sealed class ChatKitThemeGrayscaleClientConfig
{
    public int? Hue { get; init; }

    public int? Tint { get; init; }

    public int? Shade { get; init; }
}

internal sealed class ChatKitThemeAccentColorClientConfig
{
    public string? Primary { get; init; }

    public int? Level { get; init; }
}

internal sealed class ChatKitThemeSurfaceColorsClientConfig
{
    public string? Background { get; init; }

    public string? Foreground { get; init; }
}

internal sealed class ChatKitHeaderClientConfig
{
    public bool? Enabled { get; init; }

    public ChatKitHeaderActionClientConfig? LeftAction { get; init; }

    public ChatKitHeaderActionClientConfig? RightAction { get; init; }

    public ChatKitHeaderTitleClientConfig? Title { get; init; }
}

internal sealed class ChatKitHeaderActionClientConfig
{
    public string? Icon { get; init; }

    public string? OnClickHandler { get; init; }
}

internal sealed class ChatKitHeaderTitleClientConfig
{
    public bool? Enabled { get; init; }

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

    public required object Prompt { get; init; }

    public string? Icon { get; init; }
}

internal sealed class ChatKitComposerClientConfig
{
    public string? Placeholder { get; init; }

    public ChatKitComposerAttachmentsClientConfig? Attachments { get; init; }

    public IReadOnlyList<ChatKitComposerToolClientConfig>? Tools { get; init; }

    public IReadOnlyList<ChatKitComposerModelClientConfig>? Models { get; init; }

    public ChatKitComposerDictationClientConfig? Dictation { get; init; }
}

internal sealed class ChatKitComposerAttachmentsClientConfig
{
    public bool Enabled { get; init; }

    public long? MaxSize { get; init; }

    public int? MaxCount { get; init; }

    public IReadOnlyDictionary<string, string[]>? Accept { get; init; }
}

internal sealed class ChatKitComposerToolClientConfig
{
    public string? Id { get; init; }

    public string? Label { get; init; }

    public string? Icon { get; init; }

    public string? ShortLabel { get; init; }

    public string? PlaceholderOverride { get; init; }

    public bool? Pinned { get; init; }

    public bool? Persistent { get; init; }
}

internal sealed class ChatKitComposerModelClientConfig
{
    public string? Id { get; init; }

    public string? Label { get; init; }

    public string? Description { get; init; }

    public bool? Disabled { get; init; }

    public bool? Default { get; init; }
}

internal sealed class ChatKitComposerDictationClientConfig
{
    public bool Enabled { get; init; }
}

internal sealed class ChatKitFileUploadStrategyClientConfig
{
    public string? Type { get; init; }

    public string? UploadUrl { get; init; }
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
