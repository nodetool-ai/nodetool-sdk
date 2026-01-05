using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IsFile
{
    [Key(0)]
    public string path { get; set; } = @"";

    public bool Process()
    {
        return default(bool);
    }
}
