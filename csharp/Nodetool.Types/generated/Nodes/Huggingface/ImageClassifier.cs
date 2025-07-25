using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class ImageClassifier
{
    [Key(0)]
    public Nodetool.Types.HFImageClassification model { get; set; } = new Nodetool.Types.HFImageClassification();
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();

    public object Process()
    {
        return default(object);
    }
}
