using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IteratorNode
{
    [Key(0)]
    public object input_list { get; set; } = new List<object>();

    [MessagePackObject]
    public class IteratorNodeOutput
    {
        [Key(0)]
        public object output { get; set; }
        [Key(1)]
        public int index { get; set; }
        [Key(2)]
        public Nodetool.Types.Event event { get; set; }
    }

    public IteratorNodeOutput Process()
    {
        return new IteratorNodeOutput();
    }
}
