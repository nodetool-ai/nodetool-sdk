using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ListWorkspaceFiles
{
    [Key(0)]
    public string path { get; set; } = @".";
    [Key(1)]
    public string pattern { get; set; } = @"*";
    [Key(2)]
    public bool recursive { get; set; } = false;

    public string Process()
    {
        return default(string);
    }
}
