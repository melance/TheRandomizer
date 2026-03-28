using System;
using System.Collections.Generic;
using System.Text;
using Markdig;

namespace TheRandomizer.Maui.Utilities;

internal class MarkDigHelper
{
    private static MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder()
                                                       .UseAdvancedExtensions()
                                                       .UseEmojiAndSmiley(false)
                                                       .UseAutoIdentifiers()
                                                       .UseBootstrap()
                                                       .Build();

    public static String ToHtml(String markdown)
    {
        return Markdown.ToHtml(markdown, Pipeline);
    }
}

