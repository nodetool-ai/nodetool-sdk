using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Decode
{
    [Key(0)]
    public string data { get; set; } = "";

    public string Process()
    {
        return default(string);
    }
}
