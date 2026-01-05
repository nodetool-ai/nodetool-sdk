using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class ImageToText
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(1)]
    public int max_new_tokens { get; set; } = 50;
    [Key(2)]
    public Nodetool.Types.Core.HFImageToText model { get; set; } = new Nodetool.Types.Core.HFImageToText();

    public string Process()
    {
        return default(string);
    }
}
