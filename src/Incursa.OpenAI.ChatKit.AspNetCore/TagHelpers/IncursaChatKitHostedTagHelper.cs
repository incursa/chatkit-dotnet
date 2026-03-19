using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

namespace Incursa.OpenAI.ChatKit.AspNetCore.TagHelpers;

/// <summary>
/// Renders a ChatKit host element that always uses OpenAI-hosted session and action endpoints.
/// </summary>
[HtmlTargetElement("incursa-chatkit-hosted")]
public sealed class IncursaChatKitHostedTagHelper : IncursaChatKitTagHelper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IncursaChatKitHostedTagHelper" /> class.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    /// <param name="logger">The logger used for rendering failures.</param>
    public IncursaChatKitHostedTagHelper(
        IServiceProvider serviceProvider,
        ILogger<IncursaChatKitHostedTagHelper> logger)
        : base(serviceProvider, logger)
    {
    }

    internal override ChatKitHostClientConfig BuildClientConfig()
    {
        if (!string.IsNullOrWhiteSpace(ApiUrl))
        {
            throw new InvalidOperationException(
                "The <incursa-chatkit-hosted> tag helper does not support 'api-url'. Use 'session-endpoint' and 'action-endpoint', or switch to <incursa-chatkit-api>.");
        }

        if (!string.IsNullOrWhiteSpace(DomainKey))
        {
            throw new InvalidOperationException(
                "The <incursa-chatkit-hosted> tag helper does not support 'domain-key'. Use OpenAI-hosted session/action endpoints, or switch to <incursa-chatkit-api>.");
        }

        if (string.IsNullOrWhiteSpace(SessionEndpoint))
        {
            throw new InvalidOperationException(
                "The <incursa-chatkit-hosted> tag helper requires 'session-endpoint'.");
        }

        bool forwardWidgetActions = ForwardWidgetActions ?? ResolveUiOptions().ForwardWidgetActions;
        if (forwardWidgetActions && string.IsNullOrWhiteSpace(ActionEndpoint))
        {
            throw new InvalidOperationException(
                "The <incursa-chatkit-hosted> tag helper requires 'action-endpoint' when widget forwarding is enabled.");
        }

        ChatKitHostClientConfig config = BuildFlexibleClientConfig();
        return new ChatKitHostClientConfig
        {
            ApiUrl = null,
            DomainKey = null,
            SessionEndpoint = config.SessionEndpoint,
            ActionEndpoint = config.ActionEndpoint,
            Height = config.Height,
            Locale = config.Locale,
            FrameTitle = config.FrameTitle,
            InitialThread = config.InitialThread,
            ClientToolHandlers = config.ClientToolHandlers,
            EntityHandlers = config.EntityHandlers,
            WidgetActionHandler = config.WidgetActionHandler,
            Theme = config.Theme,
            Header = config.Header,
            History = config.History,
            StartScreen = config.StartScreen,
            Composer = config.Composer,
            UploadStrategy = config.UploadStrategy,
            Disclaimer = config.Disclaimer,
            Entities = config.Entities,
            ThreadItemActions = config.ThreadItemActions,
            WidgetActions = config.WidgetActions,
        };
    }
}
