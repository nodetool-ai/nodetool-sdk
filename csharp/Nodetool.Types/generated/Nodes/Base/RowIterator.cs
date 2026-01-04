using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RowIterator
{
    [Key(0)]
    public Nodetool.Types.Core.DataframeRef dataframe { get; set; } = new Nodetool.Types.Core.DataframeRef();

    [MessagePackObject]
    public class RowIteratorOutput
    {
        [Key(0)]
        public object dict { get; set; }
        [Key(1)]
        public object index { get; set; }
    }

    public RowIteratorOutput Process()
    {
        return new RowIteratorOutput();
    }
}
