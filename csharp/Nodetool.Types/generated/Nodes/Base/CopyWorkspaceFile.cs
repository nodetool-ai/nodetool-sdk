using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CopyWorkspaceFile
{
    [Key(0)]
    public string destination { get; set; } = @"";
    [Key(1)]
    public string source { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
