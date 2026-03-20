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
    /// This is required whenever <see cref="ApiUrl" /> is set.
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
    /// When set, the resolved function receives <c>(action, widgetItem)</c> on every widget action.
    /// This handler runs before server-side forwarding (see <see cref="ForwardWidgetActions" />),
    /// so throwing inside the callback prevents the action from being forwarded to the endpoint.
    /// Both a client handler and endpoint forwarding can be active simultaneously.
    /// </summary>
    [HtmlAttributeName("widget-action-handler")]
    public string? WidgetActionHandler { get; set; }

    /// <summary>
    /// Gets or sets the composer attachment enabled flag.
    /// </summary>
    [HtmlAttributeName("composer-attachments-enabled")]
    public bool? ComposerAttachmentsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum attachment size in bytes.
    /// </summary>
    [HtmlAttributeName("composer-attachments-max-size")]
    public long? ComposerAttachmentsMaxSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of composer attachments per message.
    /// </summary>
    [HtmlAttributeName("composer-attachments-max-count")]
    public int? ComposerAttachmentsMaxCount { get; set; }

    /// <summary>
    /// Gets or sets the composer dictation enabled flag.
    /// </summary>
    [HtmlAttributeName("composer-dictation-enabled")]
    public bool? ComposerDictationEnabled { get; set; }

    /// <summary>
    /// Gets or sets the upload strategy type used by direct API mode.
    /// </summary>
    [HtmlAttributeName("upload-strategy-type")]
    public string? UploadStrategyType { get; set; }

    /// <summary>
    /// Gets or sets the upload URL used by direct API mode.
    /// </summary>
    [HtmlAttributeName("upload-strategy-upload-url")]
    public string? UploadStrategyUploadUrl { get; set; }

    /// <summary>
    /// Gets or sets the color-scheme theme value.
    /// </summary>
    [HtmlAttributeName("theme")]
    public string? Theme { get; set; }

    /// <summary>
    /// Gets or sets the theme base font size in pixels.
    /// </summary>
    [HtmlAttributeName("theme-base-size")]
    public int? ThemeBaseSize { get; set; }

    /// <summary>
    /// Gets or sets the theme font family.
    /// </summary>
    [HtmlAttributeName("theme-font-family")]
    public string? ThemeFontFamily { get; set; }

    /// <summary>
    /// Gets or sets the theme monospace font family.
    /// </summary>
    [HtmlAttributeName("theme-font-family-mono")]
    public string? ThemeFontFamilyMono { get; set; }

    /// <summary>
    /// Gets or sets the theme grayscale hue.
    /// </summary>
    [HtmlAttributeName("theme-color-grayscale-hue")]
    public int? ThemeColorGrayscaleHue { get; set; }

    /// <summary>
    /// Gets or sets the theme grayscale tint.
    /// </summary>
    [HtmlAttributeName("theme-color-grayscale-tint")]
    public int? ThemeColorGrayscaleTint { get; set; }

    /// <summary>
    /// Gets or sets the theme grayscale shade.
    /// </summary>
    [HtmlAttributeName("theme-color-grayscale-shade")]
    public int? ThemeColorGrayscaleShade { get; set; }

    /// <summary>
    /// Gets or sets the theme accent primary color.
    /// </summary>
    [HtmlAttributeName("theme-color-accent-primary")]
    public string? ThemeColorAccentPrimary { get; set; }

    /// <summary>
    /// Gets or sets the theme accent level.
    /// </summary>
    [HtmlAttributeName("theme-color-accent-level")]
    public int? ThemeColorAccentLevel { get; set; }

    /// <summary>
    /// Gets or sets the theme surface background color.
    /// </summary>
    [HtmlAttributeName("theme-color-surface-background")]
    public string? ThemeColorSurfaceBackground { get; set; }

    /// <summary>
    /// Gets or sets the theme surface foreground color.
    /// </summary>
    [HtmlAttributeName("theme-color-surface-foreground")]
    public string? ThemeColorSurfaceForeground { get; set; }

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
    /// Gets or sets a value indicating whether the header title area is enabled.
    /// </summary>
    [HtmlAttributeName("header-title-enabled")]
    public bool? HeaderTitleEnabled { get; set; }

    /// <summary>
    /// Gets or sets the header left action icon.
    /// </summary>
    [HtmlAttributeName("header-left-action-icon")]
    public string? HeaderLeftActionIcon { get; set; }

    /// <summary>
    /// Gets or sets the browser lookup path for the header left action callback.
    /// </summary>
    [HtmlAttributeName("header-left-action-handler")]
    public string? HeaderLeftActionHandler { get; set; }

    /// <summary>
    /// Gets or sets the header right action icon.
    /// </summary>
    [HtmlAttributeName("header-right-action-icon")]
    public string? HeaderRightActionIcon { get; set; }

    /// <summary>
    /// Gets or sets the browser lookup path for the header right action callback.
    /// </summary>
    [HtmlAttributeName("header-right-action-handler")]
    public string? HeaderRightActionHandler { get; set; }

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
    /// When both this and <see cref="WidgetActionHandler" /> are active, the client handler runs first.
    /// Throwing inside the client handler vetoes forwarding; if the handler succeeds the action is also
    /// sent to the server endpoint. Set this to <see langword="false" /> to use a client-only handler
    /// without any server-side forwarding.
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
            UploadStrategy = string.IsNullOrWhiteSpace(apiUrl)
                ? null
                : BuildUploadStrategyConfig(uiOptions),
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
        ChatKitComposerAttachmentsClientConfig? attachments = BuildComposerAttachmentsConfig(uiOptions);
        IReadOnlyList<ChatKitComposerToolClientConfig>? tools = BuildComposerToolsConfig(uiOptions);
        IReadOnlyList<ChatKitComposerModelClientConfig>? models = BuildComposerModelsConfig(uiOptions);
        ChatKitComposerDictationClientConfig? dictation = BuildComposerDictationConfig(uiOptions);

        return string.IsNullOrWhiteSpace(placeholder)
            && attachments is null
            && (tools is null || tools.Count == 0)
            && (models is null || models.Count == 0)
            && dictation is null
            ? null
            : new ChatKitComposerClientConfig
            {
                Placeholder = placeholder,
                Attachments = attachments,
                Tools = tools,
                Models = models,
                Dictation = dictation,
            };
    }

    private ChatKitComposerAttachmentsClientConfig? BuildComposerAttachmentsConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        ChatKitComposerAttachmentsOptions attachments = uiOptions.Composer.Attachments;
        bool? enabled = ComposerAttachmentsEnabled ?? attachments.Enabled;
        long? maxSize = ComposerAttachmentsMaxSize ?? attachments.MaxSize;
        int? maxCount = ComposerAttachmentsMaxCount ?? attachments.MaxCount;
        IReadOnlyDictionary<string, string[]>? accept = attachments.Accept;

        if (enabled is null && maxSize is null && maxCount is null && accept is null)
        {
            return null;
        }

        if (enabled is null && (maxSize is not null || maxCount is not null || accept is not null))
        {
            enabled = true;
        }

        return new ChatKitComposerAttachmentsClientConfig
        {
            Enabled = enabled ?? true,
            MaxSize = maxSize,
            MaxCount = maxCount,
            Accept = accept,
        };
    }

    private IReadOnlyList<ChatKitComposerToolClientConfig>? BuildComposerToolsConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        ChatKitComposerToolClientConfig[]? mapped = uiOptions.Composer.Tools
            .Where(static tool =>
                !string.IsNullOrWhiteSpace(tool.Id) &&
                !string.IsNullOrWhiteSpace(tool.Label) &&
                !string.IsNullOrWhiteSpace(tool.Icon))
            .Select(static tool => new ChatKitComposerToolClientConfig
            {
                Id = tool.Id,
                Label = tool.Label,
                Icon = tool.Icon,
                ShortLabel = tool.ShortLabel,
                PlaceholderOverride = tool.PlaceholderOverride,
                Pinned = tool.Pinned,
                Persistent = tool.Persistent,
            })
            .ToArray();

        return mapped is { Length: > 0 } ? mapped : null;
    }

    private IReadOnlyList<ChatKitComposerModelClientConfig>? BuildComposerModelsConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        ChatKitComposerModelClientConfig[]? mapped = uiOptions.Composer.Models
            .Where(static model =>
                !string.IsNullOrWhiteSpace(model.Id) &&
                !string.IsNullOrWhiteSpace(model.Label))
            .Select(static model => new ChatKitComposerModelClientConfig
            {
                Id = model.Id,
                Label = model.Label,
                Description = model.Description,
                Disabled = model.Disabled,
                Default = model.Default,
            })
            .ToArray();

        return mapped is { Length: > 0 } ? mapped : null;
    }

    private ChatKitComposerDictationClientConfig? BuildComposerDictationConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        bool? enabled = ComposerDictationEnabled ?? uiOptions.Composer.Dictation.Enabled;
        return enabled is null
            ? null
            : new ChatKitComposerDictationClientConfig
            {
                Enabled = enabled.Value,
            };
    }

    private ChatKitFileUploadStrategyClientConfig? BuildUploadStrategyConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        string? type = FirstNonEmpty(UploadStrategyType, uiOptions.UploadStrategy.Type);
        string? uploadUrl = FirstNonEmpty(UploadStrategyUploadUrl, uiOptions.UploadStrategy.UploadUrl);

        if (string.IsNullOrWhiteSpace(type))
        {
            if (string.IsNullOrWhiteSpace(uploadUrl))
            {
                return null;
            }

            type = "direct";
        }

        if (!string.Equals(type, "direct", StringComparison.OrdinalIgnoreCase))
        {
            uploadUrl = null;
        }

        return new ChatKitFileUploadStrategyClientConfig
        {
            Type = type,
            UploadUrl = uploadUrl,
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
        bool? titleEnabled = HeaderTitleEnabled ?? uiOptions.Header.TitleEnabled;
        ChatKitHeaderActionClientConfig? leftAction = BuildHeaderAction(
            HeaderLeftActionIcon,
            HeaderLeftActionHandler,
            uiOptions.Header.LeftAction,
            "left");
        ChatKitHeaderActionClientConfig? rightAction = BuildHeaderAction(
            HeaderRightActionIcon,
            HeaderRightActionHandler,
            uiOptions.Header.RightAction,
            "right");
        if (enabled is null && titleEnabled is null && string.IsNullOrWhiteSpace(title) && leftAction is null && rightAction is null)
        {
            return null;
        }

        return new ChatKitHeaderClientConfig
        {
            Enabled = enabled,
            LeftAction = leftAction,
            RightAction = rightAction,
            Title = string.IsNullOrWhiteSpace(title)
                ? titleEnabled is null
                    ? null
                    : new ChatKitHeaderTitleClientConfig
                    {
                        Enabled = titleEnabled,
                    }
                : new ChatKitHeaderTitleClientConfig
                {
                    Enabled = titleEnabled,
                    Text = title,
                },
        };
    }

    private static ChatKitHeaderActionClientConfig? BuildHeaderAction(
        string? icon,
        string? handler,
        ChatKitHeaderActionOptions? fallback,
        string side)
    {
        string? resolvedIcon = FirstNonEmpty(icon, fallback?.Icon);
        string? resolvedHandler = FirstNonEmpty(handler, fallback?.OnClickHandler);
        if (string.IsNullOrWhiteSpace(resolvedIcon) && string.IsNullOrWhiteSpace(resolvedHandler))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(resolvedIcon) || string.IsNullOrWhiteSpace(resolvedHandler))
        {
            throw new InvalidOperationException(
                $"The ChatKit header {side} action requires both an icon and a browser callback lookup path.");
        }

        return new ChatKitHeaderActionClientConfig
        {
            Icon = resolvedIcon,
            OnClickHandler = resolvedHandler,
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
            .Where(static prompt => !string.IsNullOrWhiteSpace(prompt.Label) && HasStartPromptContent(prompt.Prompt))
            .Select(static prompt => new ChatKitStartPromptClientConfig
            {
                Label = prompt.Label!,
                Prompt = NormalizeStartPromptContent(prompt.Prompt),
                Icon = prompt.Icon,
            })
            .ToArray();

        return mapped is { Length: > 0 } ? mapped : null;
    }

    private static object NormalizeStartPromptContent(object? prompt)
    {
        if (prompt is null)
        {
            throw new InvalidOperationException("ChatKit start screen prompt content is required.");
        }

        if (prompt is string text)
        {
            return text;
        }

        if (prompt is IEnumerable<global::Incursa.OpenAI.ChatKit.UserMessageContent> content)
        {
            return content.ToArray();
        }

        throw new InvalidOperationException(
            "ChatKit start screen prompt content must be a string or a sequence of UserMessageContent values.");
    }

    private static bool HasStartPromptContent(object? prompt)
    {
        return prompt is string
            || prompt is IEnumerable<global::Incursa.OpenAI.ChatKit.UserMessageContent>;
    }

    private ChatKitThemeClientConfig? BuildThemeConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        string? colorScheme = FirstNonEmpty(Theme, uiOptions.Theme.ColorScheme);
        string? radius = FirstNonEmpty(ThemeRadius, uiOptions.Theme.Radius);
        string? density = FirstNonEmpty(ThemeDensity, uiOptions.Theme.Density);
        ChatKitThemeTypographyClientConfig? typography = BuildThemeTypographyConfig(uiOptions);
        ChatKitThemeColorClientConfig? color = BuildThemeColorConfig(uiOptions);
        if (string.IsNullOrWhiteSpace(colorScheme)
            && string.IsNullOrWhiteSpace(radius)
            && string.IsNullOrWhiteSpace(density)
            && typography is null
            && color is null)
        {
            return null;
        }

        return new ChatKitThemeClientConfig
        {
            ColorScheme = colorScheme,
            Typography = typography,
            Color = color,
            Radius = radius,
            Density = density,
        };
    }

    private ChatKitThemeTypographyClientConfig? BuildThemeTypographyConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        int? baseSize = ThemeBaseSize ?? uiOptions.Theme.Typography.BaseSize;
        string? fontFamily = FirstNonEmpty(ThemeFontFamily, uiOptions.Theme.Typography.FontFamily);
        string? fontFamilyMono = FirstNonEmpty(ThemeFontFamilyMono, uiOptions.Theme.Typography.FontFamilyMono);
        IReadOnlyList<ChatKitFontSourceClientConfig>? fontSources = BuildThemeFontSources(uiOptions);

        if (baseSize is null
            && string.IsNullOrWhiteSpace(fontFamily)
            && string.IsNullOrWhiteSpace(fontFamilyMono)
            && (fontSources is null || fontSources.Count == 0))
        {
            return null;
        }

        return new ChatKitThemeTypographyClientConfig
        {
            BaseSize = baseSize,
            FontFamily = fontFamily,
            FontFamilyMono = fontFamilyMono,
            FontSources = fontSources,
        };
    }

    private IReadOnlyList<ChatKitFontSourceClientConfig>? BuildThemeFontSources(ChatKitAspNetCoreOptions uiOptions)
    {
        ChatKitFontSourceClientConfig[]? mapped = uiOptions.Theme.Typography.FontSources
            .Where(static fontSource =>
                !string.IsNullOrWhiteSpace(fontSource.Family) &&
                !string.IsNullOrWhiteSpace(fontSource.Src))
            .Select(static fontSource => new ChatKitFontSourceClientConfig
            {
                Family = fontSource.Family,
                Src = fontSource.Src,
                Weight = fontSource.Weight,
                Style = fontSource.Style,
                Display = fontSource.Display,
                UnicodeRange = fontSource.UnicodeRange,
            })
            .ToArray();

        return mapped is { Length: > 0 } ? mapped : null;
    }

    private ChatKitThemeColorClientConfig? BuildThemeColorConfig(ChatKitAspNetCoreOptions uiOptions)
    {
        int? grayscaleHue = ThemeColorGrayscaleHue ?? uiOptions.Theme.Color.Grayscale.Hue;
        int? grayscaleTint = ThemeColorGrayscaleTint ?? uiOptions.Theme.Color.Grayscale.Tint;
        int? grayscaleShade = ThemeColorGrayscaleShade ?? uiOptions.Theme.Color.Grayscale.Shade;
        string? accentPrimary = FirstNonEmpty(ThemeColorAccentPrimary, uiOptions.Theme.Color.Accent.Primary);
        int? accentLevel = ThemeColorAccentLevel ?? uiOptions.Theme.Color.Accent.Level;
        string? surfaceBackground = FirstNonEmpty(ThemeColorSurfaceBackground, uiOptions.Theme.Color.Surface.Background);
        string? surfaceForeground = FirstNonEmpty(ThemeColorSurfaceForeground, uiOptions.Theme.Color.Surface.Foreground);

        ChatKitThemeGrayscaleClientConfig? grayscale = grayscaleHue is null && grayscaleTint is null && grayscaleShade is null
            ? null
            : new ChatKitThemeGrayscaleClientConfig
            {
                Hue = grayscaleHue,
                Tint = grayscaleTint,
                Shade = grayscaleShade,
            };

        ChatKitThemeAccentColorClientConfig? accent = string.IsNullOrWhiteSpace(accentPrimary) && accentLevel is null
            ? null
            : new ChatKitThemeAccentColorClientConfig
            {
                Primary = accentPrimary,
                Level = accentLevel,
            };

        ChatKitThemeSurfaceColorsClientConfig? surface = string.IsNullOrWhiteSpace(surfaceBackground) && string.IsNullOrWhiteSpace(surfaceForeground)
            ? null
            : new ChatKitThemeSurfaceColorsClientConfig
            {
                Background = surfaceBackground,
                Foreground = surfaceForeground,
            };

        return grayscale is null && accent is null && surface is null
            ? null
            : new ChatKitThemeColorClientConfig
            {
                Grayscale = grayscale,
                Accent = accent,
                Surface = surface,
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
