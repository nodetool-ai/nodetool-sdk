using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class SplitSentences
{
    [Key(0)]
    public int chunk_overlap { get; set; } = 5;
    [Key(1)]
    public int chunk_size { get; set; } = 40;
    [Key(2)]
    public Nodetool.Types.Core.DocumentRef document { get; set; } = new Nodetool.Types.Core.DocumentRef();

    [MessagePackObject]
    public class SplitSentencesOutput
    {
        [Key(0)]
        public string source_id { get; set; }
        [Key(1)]
        public int start_index { get; set; }
        [Key(2)]
        public string text { get; set; }
    }

    public SplitSentencesOutput Process()
    {
        return new SplitSentencesOutput();
    }
}
