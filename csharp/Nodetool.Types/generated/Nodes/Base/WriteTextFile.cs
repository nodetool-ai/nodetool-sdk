using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class WriteTextFile
{
    [Key(0)]
    public bool append { get; set; } = false;
    [Key(1)]
    public string content { get; set; } = @"";
    [Key(2)]
    public string encoding { get; set; } = @"utf-8";
    [Key(3)]
    public string path { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
