using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class TableQuestionAnswering
{
    [Key(0)]
    public Nodetool.Types.Core.DataframeRef dataframe { get; set; } = new Nodetool.Types.Core.DataframeRef();
    [Key(1)]
    public Nodetool.Types.Core.HFTableQuestionAnswering model { get; set; } = new Nodetool.Types.Core.HFTableQuestionAnswering();
    [Key(2)]
    public string question { get; set; } = @"";

    [MessagePackObject]
    public class TableQuestionAnsweringOutput
    {
        [Key(0)]
        public string aggregator { get; set; }
        [Key(1)]
        public string answer { get; set; }
        [Key(2)]
        public object cells { get; set; }
        [Key(3)]
        public object coordinates { get; set; }
    }

    public TableQuestionAnsweringOutput Process()
    {
        return new TableQuestionAnsweringOutput();
    }
}
