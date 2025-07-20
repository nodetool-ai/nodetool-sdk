using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class TableQuestionAnswering
{
    [Key(0)]
    public Nodetool.Types.HFTableQuestionAnswering model { get; set; } = new Nodetool.Types.HFTableQuestionAnswering();
    [Key(1)]
    public Nodetool.Types.DataframeRef dataframe { get; set; } = new Nodetool.Types.DataframeRef();
    [Key(2)]
    public string question { get; set; } = "";

    [MessagePackObject]
    public class TableQuestionAnsweringOutput
    {
        [Key(0)]
        public string answer { get; set; }
        [Key(1)]
        public object coordinates { get; set; }
        [Key(2)]
        public object cells { get; set; }
        [Key(3)]
        public string aggregator { get; set; }
    }

    public TableQuestionAnsweringOutput Process()
    {
        // Implementation would be generated based on node logic
        return new TableQuestionAnsweringOutput();
    }
}
