using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class ImageClassifier
{
    [Key(0)]
    public Nodetool.Types.HFImageClassification model { get; set; } = new Nodetool.Types.HFImageClassification();
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
