using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DeleteWorkspaceFile
{
    [Key(0)]
    public string path { get; set; } = @"";
    [Key(1)]
    public bool recursive { get; set; } = false;

    public object Process()
    {
        return default(object);
    }
}
