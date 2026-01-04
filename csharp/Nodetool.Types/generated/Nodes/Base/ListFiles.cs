using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ListFiles
{
    [Key(0)]
    public string folder { get; set; } = @"~";
    [Key(1)]
    public bool include_subdirectories { get; set; } = false;
    [Key(2)]
    public string pattern { get; set; } = @"*";

    public string Process()
    {
        return default(string);
    }
}
