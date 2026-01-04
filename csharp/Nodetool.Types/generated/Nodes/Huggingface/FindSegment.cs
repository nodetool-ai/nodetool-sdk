using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class FindSegment
{
    [Key(0)]
    public string segment_label { get; set; } = @"";
    [Key(1)]
    public List<Nodetool.Types.Core.ImageSegmentationResult> segments { get; set; } = new();
}
