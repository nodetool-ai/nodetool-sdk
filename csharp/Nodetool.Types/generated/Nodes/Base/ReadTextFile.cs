using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ReadTextFile
{
    [Key(0)]
    public string encoding { get; set; } = @"utf-8";
    [Key(1)]
    public string path { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
