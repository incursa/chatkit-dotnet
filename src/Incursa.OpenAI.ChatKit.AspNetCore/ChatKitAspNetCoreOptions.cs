namespace Incursa.OpenAI.ChatKit.AspNetCore;

/// <summary>
/// Configures the Razor and browser-hosting surface for ChatKit.
/// </summary>
public sealed class ChatKitAspNetCoreOptions
{
    /// <summary>
    /// Gets or sets the hosted ChatKit API URL used when the frontend should connect directly to a remote deployment.
    /// </summary>
    public string? ApiUrl { get; set; }

    /// <summary>
    /// Gets or sets the domain key sent when <see cref="ApiUrl" /> is used.
    /// </summary>
    public string? DomainKey { get; set; }

    /// <summary>
    /// Gets or sets the local session endpoint used by the packaged frontend when <see cref="ApiUrl" /> is not configured.
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
    /// Gets or sets a value indicating whether widget actions should be forwarded to <see cref="ActionEndpoint" />.
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
    /// Gets or sets the theme radius.
    /// </summary>
    public string? Radius { get; set; }

    /// <summary>
    /// Gets or sets the theme density.
    /// </summary>
    public string? Density { get; set; }
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
    /// Gets or sets the header title text.
    /// </summary>
    public string? TitleText { get; set; }
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
    /// Gets or sets the prompt text sent when the prompt is selected.
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Gets or sets the optional icon name.
    /// </summary>
    public string? Icon { get; set; }
}
