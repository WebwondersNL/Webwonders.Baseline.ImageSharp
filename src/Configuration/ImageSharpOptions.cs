using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp.Web.Middleware;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Imaging.ImageSharp;

namespace Webwonders.Baseline.ImageSharp.Configuration;

[ComposeAfter(typeof(ImageSharpComposer))]
public class ImageSharpOptions : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.Configure<ImageSharpMiddlewareOptions>(options =>
        {
            // Capture existing task to not overwrite it
            var onParseCommandsAsync = options.OnParseCommandsAsync;
            options.OnParseCommandsAsync = async imageCommandContext =>
            {
                // Custom logic before

                await onParseCommandsAsync(imageCommandContext);

                var isWebp = false;
                var excludeItem = false;

                var path = imageCommandContext.Context.Request.Path.ToString();
                isWebp = path.EndsWith("webp");

                if (!isWebp && imageCommandContext.Context.Request.GetTypedHeaders().Accept.Any(aValue => aValue.MediaType.Value == "image/webp"))
                {
                    if (!imageCommandContext.Commands.Contains("webp")
                        && !imageCommandContext.Commands.Contains("noformat")
                        && (path.EndsWith("png") || path.EndsWith("jpg") || path.EndsWith("jpeg")))
                    {
                        imageCommandContext.Commands.Remove("format");
                        imageCommandContext.Commands.Add("format", "webp");

                        if (imageCommandContext.Commands.Contains("quality") == false)
                        {
                            imageCommandContext.Commands.Add("quality", "80");
                        }

                        if (imageCommandContext.Context.Response.Headers.ContainsKey("Vary"))
                        {
                            imageCommandContext.Context.Response.Headers.Vary = "Accept";
                        }
                        else
                        {
                            imageCommandContext.Context.Response.Headers.Append("Vary", "Accept");
                        }
                    }
                }

                if (path.EndsWith("apng") || path.EndsWith("gif"))
                {
                    excludeItem = true;
                }


                bool doesntWantFormat = isWebp || imageCommandContext.Commands.TryGetValue("noformat", out string? value) || excludeItem;

                if (doesntWantFormat)
                {
                    imageCommandContext.Commands.Remove("format");
                }


                // Custom logic after
            };
        });
}