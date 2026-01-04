using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetFileSize
{
    [Key(0)]
    public string path { get; set; } = @"";

    public int Process()
    {
        return default(int);
    }
}
