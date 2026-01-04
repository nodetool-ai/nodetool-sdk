using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveCSVFile
{
    [Key(0)]
    public object data { get; set; } = new();
    [Key(1)]
    public string filename { get; set; } = @"";
    [Key(2)]
    public string folder { get; set; } = @"";

    public void Process()
    {
    }
}
