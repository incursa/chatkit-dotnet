namespace Incursa.OpenAI.ChatKit.AspNetCore;

/// <summary>
/// Configures the Razor and browser-hosting surface for ChatKit.
/// </summary>
public sealed class ChatKitAspNetCoreOptions
{
    /// <summary>
    /// Gets or sets the ChatKit API URL used when the frontend should connect directly to a custom ChatKit API endpoint.
    /// This may point at a local ASP.NET Core endpoint mapped with <c>MapChatKit(...)</c> or at another custom ChatKit deployment.
    /// Leave this unset when the browser should use OpenAI-hosted session and action endpoints instead.
    /// </summary>
    public string? ApiUrl { get; set; }

    /// <summary>
    /// Gets or sets the domain key sent when <see cref="ApiUrl" /> is used for direct ChatKit API mode.
    /// This is required whenever the packaged frontend connects directly to a ChatKit API URL.
    /// </summary>
    public string? DomainKey { get; set; }

    /// <summary>
    /// Gets or sets the local session endpoint used by the packaged frontend when <see cref="ApiUrl" /> is not configured.
    /// Use this for OpenAI-hosted integrations that issue the browser a ChatKit client secret.
    /// </summary>
    public string? SessionEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the local widget-action endpoint used by the packaged frontend when <see cref="ApiUrl" /> is not configured.
    /// </summary>
    public string? ActionEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the default rendered host height.
    /// </summary>
    public string DefaultHeight { get; set; } = "720px";

    /// <summary>
    /// Gets or sets the locale passed to the ChatKit frontend.
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets the frame title exposed by the ChatKit frontend.
    /// </summary>
    public string? FrameTitle { get; set; }

    /// <summary>
    /// Gets or sets the initial thread identifier that the frontend should open.
    /// </summary>
    public string? InitialThread { get; set; }

    /// <summary>
    /// Gets or sets the browser lookup path for client tool handler registrations.
    /// </summary>
    public string? ClientToolHandlers { get; set; }

    /// <summary>
    /// Gets or sets the browser lookup path for entity handler registrations.
    /// </summary>
    public string? EntityHandlers { get; set; }

    /// <summary>
    /// Gets or sets the browser lookup path for the widget action callback registration.
    /// When set, the resolved function receives <c>(action, widgetItem)</c> on every widget action.
    /// This handler runs before server-side forwarding (see <see cref="ForwardWidgetActions" />),
    /// so throwing inside the callback prevents the action from being forwarded to the endpoint.
    /// Both a client handler and endpoint forwarding can be active simultaneously.
    /// </summary>
    public string? WidgetActionHandler { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether widget actions should be forwarded to <see cref="ActionEndpoint" />.
    /// When both this and <see cref="WidgetActionHandler" /> are active, the client handler runs first.
    /// Throwing inside the client handler vetoes forwarding; if the handler succeeds the action is also
    /// sent to the server endpoint. Set this to <see langword="false" /> to use a client-only handler
    /// without any server-side forwarding.
    /// </summary>
    public bool ForwardWidgetActions { get; set; } = true;

    /// <summary>
    /// Gets theme defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitThemeOptions Theme { get; } = new();

    /// <summary>
    /// Gets header defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitHeaderOptions Header { get; } = new();

    /// <summary>
    /// Gets history defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitHistoryOptions History { get; } = new();

    /// <summary>
    /// Gets start-screen defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitStartScreenOptions StartScreen { get; } = new();

    /// <summary>
    /// Gets composer defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitComposerOptions Composer { get; } = new();

    /// <summary>
    /// Gets the upload strategy used when the packaged frontend connects directly to a custom ChatKit API endpoint.
    /// </summary>
    public ChatKitFileUploadStrategyOptions UploadStrategy { get; } = new();

    /// <summary>
    /// Gets disclaimer defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitDisclaimerOptions Disclaimer { get; } = new();

    /// <summary>
    /// Gets entity defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitEntitiesOptions Entities { get; } = new();

    /// <summary>
    /// Gets thread item action defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitThreadItemActionsOptions ThreadItemActions { get; } = new();
}

/// <summary>
/// Configures ChatKit theme defaults.
/// </summary>
public sealed class ChatKitThemeOptions
{
    /// <summary>
    /// Gets or sets the color scheme.
    /// </summary>
    public string? ColorScheme { get; set; }

    /// <summary>
    /// Gets typography defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitThemeTypographyOptions Typography { get; } = new();

    /// <summary>
    /// Gets color defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitThemeColorOptions Color { get; } = new();

    /// <summary>
    /// Gets or sets the theme radius.
    /// </summary>
    public string? Radius { get; set; }

    /// <summary>
    /// Gets or sets the theme density.
    /// </summary>
    public string? Density { get; set; }
}

/// <summary>
/// Configures ChatKit typography defaults.
/// </summary>
public sealed class ChatKitThemeTypographyOptions
{
    /// <summary>
    /// Gets or sets the base font size in pixels.
    /// </summary>
    public int? BaseSize { get; set; }

    /// <summary>
    /// Gets the configured font sources.
    /// </summary>
    public List<ChatKitFontSource> FontSources { get; } = [];

    /// <summary>
    /// Gets or sets the primary font family name.
    /// </summary>
    public string? FontFamily { get; set; }

    /// <summary>
    /// Gets or sets the monospace font family name.
    /// </summary>
    public string? FontFamilyMono { get; set; }
}

/// <summary>
/// Describes a webfont source used by ChatKit typography.
/// </summary>
public sealed class ChatKitFontSource
{
    /// <summary>
    /// Gets or sets the CSS font-family name.
    /// </summary>
    public string? Family { get; set; }

    /// <summary>
    /// Gets or sets the source URL for the font file.
    /// </summary>
    public string? Src { get; set; }

    /// <summary>
    /// Gets or sets the font weight.
    /// </summary>
    public object? Weight { get; set; }

    /// <summary>
    /// Gets or sets the font style.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// Gets or sets the font rendering behavior.
    /// </summary>
    public string? Display { get; set; }

    /// <summary>
    /// Gets or sets the optional unicode range descriptor.
    /// </summary>
    public string? UnicodeRange { get; set; }
}

/// <summary>
/// Configures ChatKit color defaults.
/// </summary>
public sealed class ChatKitThemeColorOptions
{
    /// <summary>
    /// Gets grayscale defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitThemeGrayscaleOptions Grayscale { get; } = new();

    /// <summary>
    /// Gets accent defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitThemeAccentColorOptions Accent { get; } = new();

    /// <summary>
    /// Gets surface color defaults for rendered ChatKit hosts.
    /// </summary>
    public ChatKitThemeSurfaceColorsOptions Surface { get; } = new();
}

/// <summary>
/// Configures ChatKit grayscale defaults.
/// </summary>
public sealed class ChatKitThemeGrayscaleOptions
{
    /// <summary>
    /// Gets or sets the hue in degrees.
    /// </summary>
    public int? Hue { get; set; }

    /// <summary>
    /// Gets or sets the tint step.
    /// </summary>
    public int? Tint { get; set; }

    /// <summary>
    /// Gets or sets the optional shade adjustment.
    /// </summary>
    public int? Shade { get; set; }
}

/// <summary>
/// Configures ChatKit accent color defaults.
/// </summary>
public sealed class ChatKitThemeAccentColorOptions
{
    /// <summary>
    /// Gets or sets the accent primary color.
    /// </summary>
    public string? Primary { get; set; }

    /// <summary>
    /// Gets or sets the accent palette intensity level.
    /// </summary>
    public int? Level { get; set; }
}

/// <summary>
/// Configures ChatKit surface colors.
/// </summary>
public sealed class ChatKitThemeSurfaceColorsOptions
{
    /// <summary>
    /// Gets or sets the surface background color.
    /// </summary>
    public string? Background { get; set; }

    /// <summary>
    /// Gets or sets the surface foreground color.
    /// </summary>
    public string? Foreground { get; set; }
}

/// <summary>
/// Configures ChatKit header defaults.
/// </summary>
public sealed class ChatKitHeaderOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the header should be shown.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the optional custom action shown on the left side of the header.
    /// </summary>
    public ChatKitHeaderActionOptions? LeftAction { get; set; }

    /// <summary>
    /// Gets or sets the optional custom action shown on the right side of the header.
    /// </summary>
    public ChatKitHeaderActionOptions? RightAction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the header title area should be shown.
    /// </summary>
    public bool? TitleEnabled { get; set; }

    /// <summary>
    /// Gets or sets the header title text.
    /// </summary>
    public string? TitleText { get; set; }
}

/// <summary>
/// Configures a custom ChatKit header action.
/// </summary>
public sealed class ChatKitHeaderActionOptions
{
    /// <summary>
    /// Gets or sets the header icon name.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the browser lookup path used to resolve the header action callback.
    /// </summary>
    public string? OnClickHandler { get; set; }
}

/// <summary>
/// Configures ChatKit history defaults.
/// </summary>
public sealed class ChatKitHistoryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether history navigation is enabled.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether delete actions should be shown in history.
    /// </summary>
    public bool? ShowDelete { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether rename actions should be shown in history.
    /// </summary>
    public bool? ShowRename { get; set; }
}

/// <summary>
/// Configures ChatKit start-screen defaults.
/// </summary>
public sealed class ChatKitStartScreenOptions
{
    /// <summary>
    /// Gets or sets the greeting rendered on the start screen.
    /// </summary>
    public string? Greeting { get; set; }

    /// <summary>
    /// Gets the configured starter prompts.
    /// </summary>
    public List<ChatKitStartPrompt> Prompts { get; } = [];
}

/// <summary>
/// Configures ChatKit composer defaults.
/// </summary>
public sealed class ChatKitComposerOptions
{
    /// <summary>
    /// Gets or sets the placeholder text displayed in the composer.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Gets attachment defaults for the composer.
    /// </summary>
    public ChatKitComposerAttachmentsOptions Attachments { get; } = new();

    /// <summary>
    /// Gets the selectable composer tools.
    /// </summary>
    public List<ChatKitComposerTool> Tools { get; } = [];

    /// <summary>
    /// Gets the selectable composer models.
    /// </summary>
    public List<ChatKitComposerModel> Models { get; } = [];

    /// <summary>
    /// Gets dictation defaults for the composer.
    /// </summary>
    public ChatKitComposerDictationOptions Dictation { get; } = new();
}

/// <summary>
/// Configures ChatKit composer attachment defaults.
/// </summary>
public sealed class ChatKitComposerAttachmentsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether attachments are enabled in the composer.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum file size in bytes.
    /// </summary>
    public long? MaxSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of attachments per message.
    /// </summary>
    public int? MaxCount { get; set; }

    /// <summary>
    /// Gets or sets the accepted file type map keyed by MIME type or extension group.
    /// </summary>
    public Dictionary<string, string[]>? Accept { get; set; }
}

/// <summary>
/// Configures a selectable ChatKit composer tool.
/// </summary>
public sealed class ChatKitComposerTool
{
    /// <summary>
    /// Gets or sets the tool identifier.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the tool label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the tool icon.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the short label displayed in the composer.
    /// </summary>
    public string? ShortLabel { get; set; }

    /// <summary>
    /// Gets or sets the placeholder override shown when the tool is selected.
    /// </summary>
    public string? PlaceholderOverride { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tool is pinned to the composer.
    /// </summary>
    public bool? Pinned { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tool remains selected after submit.
    /// </summary>
    public bool? Persistent { get; set; }
}

/// <summary>
/// Configures a selectable ChatKit composer model.
/// </summary>
public sealed class ChatKitComposerModel
{
    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the model label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the optional helper text.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model is disabled.
    /// </summary>
    public bool? Disabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model is selected by default.
    /// </summary>
    public bool? Default { get; set; }
}

/// <summary>
/// Configures ChatKit composer dictation defaults.
/// </summary>
public sealed class ChatKitComposerDictationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether dictation is enabled.
    /// </summary>
    public bool? Enabled { get; set; }
}

/// <summary>
/// Configures how the browser uploads files for the custom ChatKit API endpoint.
/// </summary>
public sealed class ChatKitFileUploadStrategyOptions
{
    /// <summary>
    /// Gets or sets the upload strategy type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the upload URL for direct uploads.
    /// </summary>
    public string? UploadUrl { get; set; }
}

/// <summary>
/// Configures ChatKit disclaimer defaults.
/// </summary>
public sealed class ChatKitDisclaimerOptions
{
    /// <summary>
    /// Gets or sets the markdown text displayed below the composer.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the disclaimer should use high-contrast rendering.
    /// </summary>
    public bool? HighContrast { get; set; }
}

/// <summary>
/// Configures ChatKit entity defaults.
/// </summary>
public sealed class ChatKitEntitiesOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the composer should show the entity insertion menu.
    /// </summary>
    public bool? ShowComposerMenu { get; set; }
}

/// <summary>
/// Configures ChatKit thread item action defaults.
/// </summary>
public sealed class ChatKitThreadItemActionsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether feedback actions should be shown.
    /// </summary>
    public bool? Feedback { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether retry actions should be shown.
    /// </summary>
    public bool? Retry { get; set; }
}

/// <summary>
/// Represents a start-screen prompt exposed by the packaged ChatKit frontend.
/// </summary>
public sealed class ChatKitStartPrompt
{
    /// <summary>
    /// Gets or sets the prompt label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the prompt content sent when the prompt is selected.
    /// Use a string for plain text or a <see cref="Incursa.OpenAI.ChatKit.UserMessageContent" /> sequence for rich content.
    /// </summary>
    public object? Prompt { get; set; }

    /// <summary>
    /// Gets or sets the optional icon name.
    /// </summary>
    public string? Icon { get; set; }
}
