using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class ImageToText
{
    [Key(0)]
    public Nodetool.Types.HFImageToText model { get; set; } = new Nodetool.Types.HFImageToText();
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public int max_new_tokens { get; set; } = 50;

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
