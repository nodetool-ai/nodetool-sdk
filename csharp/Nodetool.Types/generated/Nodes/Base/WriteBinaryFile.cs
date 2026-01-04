using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class WriteBinaryFile
{
    [Key(0)]
    public string content { get; set; } = @"";
    [Key(1)]
    public string path { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
