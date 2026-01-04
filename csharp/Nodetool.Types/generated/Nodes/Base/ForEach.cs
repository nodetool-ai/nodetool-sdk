using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ForEach
{
    [Key(0)]
    public object input_list { get; set; } = new();

    [MessagePackObject]
    public class ForEachOutput
    {
        [Key(0)]
        public int index { get; set; }
        [Key(1)]
        public object output { get; set; }
    }

    public ForEachOutput Process()
    {
        return new ForEachOutput();
    }
}
