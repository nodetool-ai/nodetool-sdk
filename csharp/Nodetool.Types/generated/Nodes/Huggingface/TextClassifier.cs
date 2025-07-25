using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class TextClassifier
{
    [Key(0)]
    public Nodetool.Types.HFTextClassification model { get; set; } = new Nodetool.Types.HFTextClassification();
    [Key(1)]
    public string prompt { get; set; } = "";

    public object Process()
    {
        return default(object);
    }
}
