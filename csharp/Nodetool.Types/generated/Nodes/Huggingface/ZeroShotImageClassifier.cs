using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class ZeroShotImageClassifier
{
    [Key(0)]
    public string candidate_labels { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public Nodetool.Types.Core.HFZeroShotImageClassification model { get; set; } = new Nodetool.Types.Core.HFZeroShotImageClassification();
}
