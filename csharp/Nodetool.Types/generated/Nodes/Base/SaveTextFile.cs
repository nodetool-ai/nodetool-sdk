using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveTextFile
{
    [Key(0)]
    public string folder { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"%Y-%m-%d-%H-%M-%S.txt";
    [Key(2)]
    public string text { get; set; } = @"";

    public Nodetool.Types.Core.TextRef Process()
    {
        return default(Nodetool.Types.Core.TextRef);
    }
}
