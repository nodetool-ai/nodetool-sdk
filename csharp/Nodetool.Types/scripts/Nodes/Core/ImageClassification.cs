using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class ImageClassification
{
    [Key(0)]
    public Nodetool.Types.InferenceProviderImageClassificationModel model { get; set; } = new Nodetool.Types.InferenceProviderImageClassificationModel();
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public object function_to_apply { get; set; }
    [Key(3)]
    public int top_k { get; set; } = 1;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
