using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Screenshot
{
    [Key(0)]
    public string output_file { get; set; } = @"screenshot.png";
    [Key(1)]
    public string selector { get; set; } = @"";
    [Key(2)]
    public int timeout { get; set; } = 30000;
    [Key(3)]
    public string url { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
