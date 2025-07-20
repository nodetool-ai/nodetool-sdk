using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class If
{
    [Key(0)]
    public bool condition { get; set; } = false;
    [Key(1)]
    public object value { get; set; } = null;

    [MessagePackObject]
    public class IfOutput
    {
        [Key(0)]
        public object if_true { get; set; }
        [Key(1)]
        public object if_false { get; set; }
    }

    public IfOutput Process()
    {
        // Implementation would be generated based on node logic
        return new IfOutput();
    }
}
