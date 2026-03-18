using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Incursa.OpenAI.ChatKit.AspNetCore.TagHelpers;

/// <summary>
/// Renders the CSS and JavaScript assets required by the packaged ChatKit frontend.
/// </summary>
[HtmlTargetElement("incursa-chatkit-assets")]
public sealed class IncursaChatKitAssetsTagHelper : TagHelper
{
    private const string CssRenderedKey = "__IncursaOpenAIChatKitAssetsCssRendered";
    private const string JsRenderedKey = "__IncursaOpenAIChatKitAssetsJsRendered";
    private const string CdnRenderedKey = "__IncursaOpenAIChatKitAssetsCdnRendered";
    private const string AssetBasePath = "/_content/Incursa.OpenAI.ChatKit.AspNetCore/chatkit";

    /// <summary>
    /// Gets or sets a value indicating whether the packaged stylesheet should be rendered.
    /// </summary>
    [HtmlAttributeName("include-css")]
    public bool IncludeCss { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the packaged JavaScript runtime should be rendered.
    /// </summary>
    [HtmlAttributeName("include-js")]
    public bool IncludeJs { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the ChatKit CDN script should be rendered.
    /// </summary>
    [HtmlAttributeName("include-cdn")]
    public bool IncludeCdn { get; set; } = true;

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;

        List<string> parts = [];

        if (IncludeCss && !context.Items.ContainsKey(CssRenderedKey))
        {
            context.Items[CssRenderedKey] = true;
            parts.Add($"<link rel=\"stylesheet\" href=\"{AssetBasePath}/chatkit.css\" />");
        }

        if (IncludeCdn && !context.Items.ContainsKey(CdnRenderedKey))
        {
            context.Items[CdnRenderedKey] = true;
            parts.Add("<script src=\"https://cdn.platform.openai.com/deployments/chatkit/chatkit.js\"></script>");
        }

        if (IncludeJs && !context.Items.ContainsKey(JsRenderedKey))
        {
            context.Items[JsRenderedKey] = true;
            parts.Add($"<script type=\"module\" src=\"{AssetBasePath}/chatkit.js\"></script>");
        }

        if (parts.Count == 0)
        {
            output.SuppressOutput();
            return;
        }

        output.Content.SetHtmlContent(string.Join(string.Empty, parts));
    }
}
