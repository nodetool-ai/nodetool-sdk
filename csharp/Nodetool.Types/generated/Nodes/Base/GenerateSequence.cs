using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GenerateSequence
{
    [Key(0)]
    public int start { get; set; } = 0;
    [Key(1)]
    public int step { get; set; } = 1;
    [Key(2)]
    public int stop { get; set; } = 0;

    public int Process()
    {
        return default(int);
    }
}
