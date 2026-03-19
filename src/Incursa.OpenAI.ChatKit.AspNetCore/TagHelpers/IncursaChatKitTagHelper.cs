using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

namespace Incursa.OpenAI.ChatKit.AspNetCore.TagHelpers;

/// <summary>
/// Requires callers to choose an explicit ChatKit host mode tag helper.
/// </summary>
[HtmlTargetElement("incursa-chatkit")]
public class IncursaChatKitTagHelper : IncursaChatKitTagHelperBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IncursaChatKitTagHelper" /> class.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    /// <param name="logger">The logger used for rendering failures.</param>
    public IncursaChatKitTagHelper(
        IServiceProvider serviceProvider,
        ILogger<IncursaChatKitTagHelper> logger)
        : this(serviceProvider, (ILogger)logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IncursaChatKitTagHelper" /> class.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    /// <param name="logger">The logger used for rendering failures.</param>
    protected IncursaChatKitTagHelper(
        IServiceProvider serviceProvider,
        ILogger logger)
        : base(serviceProvider, logger)
    {
    }

    /// <summary>
    /// Gets or sets the local session endpoint used by the packaged frontend.
    /// Use this for OpenAI-hosted integrations that issue the browser a ChatKit client secret.
    /// </summary>
    [HtmlAttributeName("session-endpoint")]
    public string? SessionEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the ChatKit API URL used when connecting directly to a custom ChatKit API endpoint.
    /// Setting this switches the packaged frontend out of session-endpoint mode.
    /// This may point at an endpoint mapped with <c>MapChatKit(...)</c>.
    /// </summary>
    [HtmlAttributeName("api-url")]
    public string? ApiUrl { get; set; }

    /// <summary>
    /// Gets or sets the domain key used with <see cref="ApiUrl" /> in direct ChatKit API mode.
    /// </summary>
    [HtmlAttributeName("domain-key")]
    public string? DomainKey { get; set; }

    /// <summary>
    /// Gets or sets the local widget-action endpoint used by the packaged frontend.
    /// </summary>
    [HtmlAttributeName("action-endpoint")]
    public string? ActionEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the frontend locale.
    /// </summary>
    [HtmlAttributeName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets the frame title exposed by the frontend.
    /// </summary>
    [HtmlAttributeName("frame-title")]
    public string? FrameTitle { get; set; }

    /// <summary>
    /// Gets or sets the initial thread identifier.
    /// </summary>
    [HtmlAttributeName("initial-thread")]
    public string? InitialThread { get; set; }

    /// <summary>
    /// Gets or sets the browser lookup path for client tool handlers.
    /// </summary>
    [HtmlAttributeName("client-tool-handlers")]
    public string? ClientToolHandlers { get; set; }

    /// <summary>
    /// Gets or sets the browser lookup path for entity handlers.
    /// </summary>
    [HtmlAttributeName("entity-handlers")]
    public string? EntityHandlers { get; set; }

    /// <summary>
    /// Gets or sets the browser lookup path for the widget action callback.
    /// </summary>
    [HtmlAttributeName("widget-action-handler")]
    public string? WidgetActionHandler { get; set; }

    /// <summary>
    /// Gets or sets the color-scheme theme value.
    /// </summary>
    [HtmlAttributeName("theme")]
    public string? Theme { get; set; }

    /// <summary>
    /// Gets or sets the theme radius value.
    /// </summary>
    [HtmlAttributeName("theme-radius")]
    public string? ThemeRadius { get; set; }

    /// <summary>
    /// Gets or sets the theme density value.
    /// </summary>
    [HtmlAttributeName("theme-density")]
    public string? ThemeDensity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the frontend header is enabled.
    /// </summary>
    [HtmlAttributeName("header-enabled")]
    public bool? HeaderEnabled { get; set; }

    /// <summary>
    /// Gets or sets the header title text.
    /// </summary>
    [HtmlAttributeName("header-title")]
    public string? HeaderTitle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether thread history is enabled.
    /// </summary>
    [HtmlAttributeName("history-enabled")]
    public bool? HistoryEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether history delete actions are shown.
    /// </summary>
    [HtmlAttributeName("history-show-delete")]
    public bool? HistoryShowDelete { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether history rename actions are shown.
    /// </summary>
    [HtmlAttributeName("history-show-rename")]
    public bool? HistoryShowRename { get; set; }

    /// <summary>
    /// Gets or sets the start-screen greeting.
    /// </summary>
    [HtmlAttributeName("greeting")]
    public string? Greeting { get; set; }

    /// <summary>
    /// Gets or sets the start-screen starter prompts.
    /// </summary>
    [HtmlAttributeName("starter-prompts")]
    public IEnumerable<ChatKitStartPrompt>? StarterPrompts { get; set; }

    /// <summary>
    /// Gets or sets the composer placeholder.
    /// </summary>
    [HtmlAttributeName("placeholder")]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the disclaimer text rendered below the composer.
    /// </summary>
    [HtmlAttributeName("disclaimer-text")]
    public string? DisclaimerText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the disclaimer uses high-contrast rendering.
    /// </summary>
    [HtmlAttributeName("disclaimer-high-contrast")]
    public bool? DisclaimerHighContrast { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the composer should show the entity insertion menu.
    /// </summary>
    [HtmlAttributeName("entity-show-composer-menu")]
    public bool? EntityShowComposerMenu { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether feedback actions are enabled.
    /// </summary>
    [HtmlAttributeName("feedback-enabled")]
    public bool? FeedbackEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether retry actions are enabled.
    /// </summary>
    [HtmlAttributeName("retry-enabled")]
    public bool? RetryEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether widget actions should be forwarded to the action endpoint.
    /// </summary>
    [HtmlAttributeName("forward-widget-actions")]
    public bool? ForwardWidgetActions { get; set; }

    internal override ChatKitHostClientConfig BuildClientConfig()
    {
        throw new InvalidOperationException(
            "Use <incursa-chatkit-api> for a custom ChatKit API endpoint or <incursa-chatkit-hosted> for OpenAI-hosted session/action endpoints.");
    }

    /// <summary>
    /// Builds the flexible ChatKit host configuration used by the explicit mode tag helpers.
    /// </summary>
    /// <returns>The serialized browser host configuration.</returns>
    internal ChatKitHostClientConfig BuildFlexibleClientConfig()
    {
        ChatKitAspNetCoreOptions uiOptions = ResolveUiOptions();
        string? apiUrl = ResolveApiUrl(ApiUrl);
        bool forwardWidgetActions = ForwardWidgetActions ?? uiOptions.ForwardWidgetActions;

        return new ChatKitHostClientConfig
        {
            ApiUrl = apiUrl,
            DomainKey = ResolveDomainKey(DomainKey),
            SessionEndpoint = string.IsNullOrWhiteSpace(apiUrl)
                ? ResolveSessionEndpoint(SessionEndpoint)
                : null,
            ActionEndpoint = string.IsNullOrWhiteSpace(apiUrl)
                ? ResolveActionEndpoint(ActionEndpoint, forwardWidgetActions)
                : null,
            Height = ResolveHeight(Height),
            Locale = FirstNonEmpty(Locale, uiOptions.Locale),
            FrameTitle = FirstNonEmpty(FrameTitle, uiOptions.FrameTitle),
            InitialThread = FirstNonEmpty(InitialThread, uiOptions.InitialThread),
            ClientToolHandlers = FirstNonEmpty(ClientToolHandlers, uiOptions.ClientToolHandlers),
            EntityHandlers = FirstNonEmpty(EntityHandlers, uiOptions.EntityHandlers),
            WidgetActionHandler = FirstNonEmpty(WidgetActionHandler, uiOptions.WidgetActionHandler),
            Theme = BuildThemeConfig(uiOptions),
            Header = BuildHeaderConfig(uiOptions),
            History = BuildHistoryConfig(uiOptions),
            StartScreen = BuildStartScreenConfig(uiOptions),
            Composer = BuildComposerConfig(uiOptions),
            Disclaimer = BuildDisclaimerConfig(uiOptions),
            Entities = BuildEntitiesConfig(uiOptions),
            ThreadItemActions = BuildThreadItemActionsConfig(uiOptions),
            WidgetActions = new ChatKitWidgetActionsClientConfig
            {
                ForwardToEndpoint = forwardWidgetActions,
            },
        };
    }

    private ChatKitComposerClientConfig? BuildComposerConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        string? placeholder = FirstNonEmpty(Placeholder, uiOptions.Composer.Placeholder);
        return string.IsNullOrWhiteSpace(placeholder)
            ? null
            : new ChatKitComposerClientConfig
            {
                Placeholder = placeholder,
            };
    }

    private ChatKitDisclaimerClientConfig? BuildDisclaimerConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        string? text = FirstNonEmpty(DisclaimerText, uiOptions.Disclaimer.Text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return new ChatKitDisclaimerClientConfig
        {
            Text = text,
            HighContrast = DisclaimerHighContrast ?? uiOptions.Disclaimer.HighContrast,
        };
    }

    private ChatKitEntitiesClientConfig? BuildEntitiesConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        bool? showComposerMenu = EntityShowComposerMenu ?? uiOptions.Entities.ShowComposerMenu;
        return showComposerMenu is null
            ? null
            : new ChatKitEntitiesClientConfig
            {
                ShowComposerMenu = showComposerMenu,
            };
    }

    private ChatKitHeaderClientConfig? BuildHeaderConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        bool? enabled = HeaderEnabled ?? uiOptions.Header.Enabled;
        string? title = FirstNonEmpty(HeaderTitle, uiOptions.Header.TitleText);
        if (enabled is null && string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        return new ChatKitHeaderClientConfig
        {
            Enabled = enabled,
            Title = string.IsNullOrWhiteSpace(title)
                ? null
                : new ChatKitHeaderTitleClientConfig
                {
                    Text = title,
                },
        };
    }

    private ChatKitHistoryClientConfig? BuildHistoryConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        bool? enabled = HistoryEnabled ?? uiOptions.History.Enabled;
        bool? showDelete = HistoryShowDelete ?? uiOptions.History.ShowDelete;
        bool? showRename = HistoryShowRename ?? uiOptions.History.ShowRename;
        if (enabled is null && showDelete is null && showRename is null)
        {
            return null;
        }

        return new ChatKitHistoryClientConfig
        {
            Enabled = enabled,
            ShowDelete = showDelete,
            ShowRename = showRename,
        };
    }

    private ChatKitStartScreenClientConfig? BuildStartScreenConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        string? greeting = FirstNonEmpty(Greeting, uiOptions.StartScreen.Greeting);
        IReadOnlyList<ChatKitStartPromptClientConfig>? prompts = BuildStartPrompts(uiOptions);
        if (string.IsNullOrWhiteSpace(greeting) && (prompts is null || prompts.Count == 0))
        {
            return null;
        }

        return new ChatKitStartScreenClientConfig
        {
            Greeting = greeting,
            Prompts = prompts,
        };
    }

    private IReadOnlyList<ChatKitStartPromptClientConfig>? BuildStartPrompts(ChatKitAspNetCoreOptions uiOptions)
    {
        IEnumerable<ChatKitStartPrompt>? prompts = StarterPrompts ?? uiOptions.StartScreen.Prompts;
        ChatKitStartPromptClientConfig[]? mapped = prompts?
            .Where(static prompt => !string.IsNullOrWhiteSpace(prompt.Label) && !string.IsNullOrWhiteSpace(prompt.Prompt))
            .Select(static prompt => new ChatKitStartPromptClientConfig
            {
                Label = prompt.Label!,
                Prompt = prompt.Prompt!,
                Icon = prompt.Icon,
            })
            .ToArray();

        return mapped is { Length: > 0 } ? mapped : null;
    }

    private ChatKitThemeClientConfig? BuildThemeConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        string? colorScheme = FirstNonEmpty(Theme, uiOptions.Theme.ColorScheme);
        string? radius = FirstNonEmpty(ThemeRadius, uiOptions.Theme.Radius);
        string? density = FirstNonEmpty(ThemeDensity, uiOptions.Theme.Density);
        if (string.IsNullOrWhiteSpace(colorScheme) && string.IsNullOrWhiteSpace(radius) && string.IsNullOrWhiteSpace(density))
        {
            return null;
        }

        return new ChatKitThemeClientConfig
        {
            ColorScheme = colorScheme,
            Radius = radius,
            Density = density,
        };
    }

    private ChatKitThreadItemActionsClientConfig? BuildThreadItemActionsConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        bool? feedback = FeedbackEnabled ?? uiOptions.ThreadItemActions.Feedback;
        bool? retry = RetryEnabled ?? uiOptions.ThreadItemActions.Retry;
        if (feedback is null && retry is null)
        {
            return null;
        }

        return new ChatKitThreadItemActionsClientConfig
        {
            Feedback = feedback,
            Retry = retry,
        };
    }

    private static string? FirstNonEmpty(string? preferred, string? fallback)
    {
        return !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback;
    }
}
