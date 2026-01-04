using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CreateWorkspaceDirectory
{
    [Key(0)]
    public string path { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
