using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class LoadImageTextToTextModel
{
    [Key(0)]
    public string repo_id { get; set; } = @"";

    public Nodetool.Types.Core.HFImageTextToText Process()
    {
        return default(Nodetool.Types.Core.HFImageTextToText);
    }
}
