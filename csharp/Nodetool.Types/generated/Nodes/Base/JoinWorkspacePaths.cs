using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class JoinWorkspacePaths
{
    [Key(0)]
    public object paths { get; set; } = new();

    public string Process()
    {
        return default(string);
    }
}
