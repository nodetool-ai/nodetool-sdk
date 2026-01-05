using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class PathToString
{
    [Key(0)]
    public string file_path { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
