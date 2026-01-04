using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CreateDirectory
{
    [Key(0)]
    public bool exist_ok { get; set; } = true;
    [Key(1)]
    public string path { get; set; } = @"";

    public void Process()
    {
    }
}
