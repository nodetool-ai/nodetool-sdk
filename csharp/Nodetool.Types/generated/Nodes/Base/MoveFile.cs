using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class MoveFile
{
    [Key(0)]
    public string destination_path { get; set; } = @"";
    [Key(1)]
    public string source_path { get; set; } = @"";

    public void Process()
    {
    }
}
