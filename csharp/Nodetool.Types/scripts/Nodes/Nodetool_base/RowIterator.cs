using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class RowIterator
{
    [Key(0)]
    public Nodetool.Types.DataframeRef dataframe { get; set; } = new Nodetool.Types.DataframeRef();

    [MessagePackObject]
    public class RowIteratorOutput
    {
        [Key(0)]
        public object dict { get; set; }
        [Key(1)]
        public int index { get; set; }
    }

    public RowIteratorOutput Process()
    {
        // Implementation would be generated based on node logic
        return new RowIteratorOutput();
    }
}
