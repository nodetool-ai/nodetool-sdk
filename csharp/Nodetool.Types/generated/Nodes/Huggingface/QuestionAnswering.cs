using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class QuestionAnswering
{
    [Key(0)]
    public string context { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.HFQuestionAnswering model { get; set; } = new Nodetool.Types.Core.HFQuestionAnswering();
    [Key(2)]
    public string question { get; set; } = @"";

    [MessagePackObject]
    public class QuestionAnsweringOutput
    {
        [Key(0)]
        public string answer { get; set; }
        [Key(1)]
        public int end { get; set; }
        [Key(2)]
        public double score { get; set; }
        [Key(3)]
        public int start { get; set; }
    }

    public QuestionAnsweringOutput Process()
    {
        return new QuestionAnsweringOutput();
    }
}
