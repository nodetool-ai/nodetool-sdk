using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class SentenceSimilarity
{
    [Key(0)]
    public string inputs { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.HFSentenceSimilarity model { get; set; } = new Nodetool.Types.Core.HFSentenceSimilarity();

    public Nodetool.Types.Core.NPArray Process()
    {
        return default(Nodetool.Types.Core.NPArray);
    }
}
