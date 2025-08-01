using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CollectorNode
{
    [Key(0)]
    public object input_item { get; set; } = null;
    [Key(1)]
    public Nodetool.Types.Event event { get; set; } = new Nodetool.Types.Event();

    [MessagePackObject]
    public class CollectorNodeOutput
    {
        [Key(0)]
        public object output { get; set; }
        [Key(1)]
        public Nodetool.Types.Event event { get; set; }
    }

    public CollectorNodeOutput Process()
    {
        return new CollectorNodeOutput();
    }
}
