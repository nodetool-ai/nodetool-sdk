using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class ImageTextToText
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(1)]
    public int max_new_tokens { get; set; } = 256;
    [Key(2)]
    public Nodetool.Types.Core.HFImageTextToText model { get; set; } = new Nodetool.Types.Core.HFImageTextToText();
    [Key(3)]
    public string prompt { get; set; } = @"Describe this image.";

    public string Process()
    {
        return default(string);
    }
}
