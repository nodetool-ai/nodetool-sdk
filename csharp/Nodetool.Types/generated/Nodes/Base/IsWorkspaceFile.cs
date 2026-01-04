using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IsWorkspaceFile
{
    [Key(0)]
    public string path { get; set; } = @"";

    public bool Process()
    {
        return default(bool);
    }
}
