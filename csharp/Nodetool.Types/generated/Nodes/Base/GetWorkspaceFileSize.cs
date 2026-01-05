using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetWorkspaceFileSize
{
    [Key(0)]
    public string path { get; set; } = @"";

    public int Process()
    {
        return default(int);
    }
}
