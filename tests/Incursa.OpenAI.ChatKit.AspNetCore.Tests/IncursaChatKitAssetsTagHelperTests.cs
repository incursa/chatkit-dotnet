using Incursa.OpenAI.ChatKit.AspNetCore.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Incursa.OpenAI.ChatKit.AspNetCore.Tests;

[Trait("Category", "Unit")]
public sealed class IncursaChatKitAssetsTagHelperTests
{
    /// <summary>The assets tag helper emits each required asset only once per rendering context.</summary>
    /// <intent>Protect the public Razor wrapper surface added to the ASP.NET Core package.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>CSS, package JS, and CDN JS are emitted on first render and suppressed on the second render in the same context.</behavior>
    [Fact]
    public void Process_RendersEachAssetOnlyOncePerContext()
    {
        IncursaChatKitAssetsTagHelper tagHelper = new();
        Dictionary<object, object> items = [];

        TagHelperOutput firstOutput = CreateOutput();
        tagHelper.Process(CreateContext(items), firstOutput);

        string firstMarkup = firstOutput.Content.GetContent();
        Assert.Contains("/_content/Incursa.OpenAI.ChatKit.AspNetCore/chatkit/chatkit.css", firstMarkup, StringComparison.Ordinal);
        Assert.Contains("/_content/Incursa.OpenAI.ChatKit.AspNetCore/chatkit/chatkit.js", firstMarkup, StringComparison.Ordinal);
        Assert.Contains("https://cdn.platform.openai.com/deployments/chatkit/chatkit.js", firstMarkup, StringComparison.Ordinal);

        TagHelperOutput secondOutput = CreateOutput();
        tagHelper.Process(CreateContext(items), secondOutput);

        Assert.Null(secondOutput.TagName);
        Assert.Equal(string.Empty, secondOutput.Content.GetContent());
    }

    private static TagHelperContext CreateContext(IDictionary<object, object> items)
    {
        return new TagHelperContext(
            tagName: "incursa-chatkit-assets",
            allAttributes: new TagHelperAttributeList(),
            items: items,
            uniqueId: Guid.NewGuid().ToString("N"));
    }

    private static TagHelperOutput CreateOutput()
    {
        return new TagHelperOutput(
            "incursa-chatkit-assets",
            attributes: [],
            getChildContentAsync: (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }
}
