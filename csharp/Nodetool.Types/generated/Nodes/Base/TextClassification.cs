using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class TextClassification
{
    [Key(0)]
    public Nodetool.Types.InferenceProviderTextClassificationModel model { get; set; } = new Nodetool.Types.InferenceProviderTextClassificationModel();
    [Key(1)]
    public string text { get; set; } = "";
    [Key(2)]
    public object function_to_apply { get; set; }
    [Key(3)]
    public int top_k { get; set; } = 1;

    public object Process()
    {
        return default(object);
    }
}
