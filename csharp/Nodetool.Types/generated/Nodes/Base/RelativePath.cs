using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RelativePath
{
    [Key(0)]
    public string start_path { get; set; } = @".";
    [Key(1)]
    public string target_path { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
