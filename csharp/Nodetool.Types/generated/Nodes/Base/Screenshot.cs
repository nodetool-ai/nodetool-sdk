using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class Screenshot
{
    [Key(0)]
    public string url { get; set; } = "";
    [Key(1)]
    public string selector { get; set; } = "";
    [Key(2)]
    public Nodetool.Types.FilePath output_file { get; set; } = new Nodetool.Types.FilePath();
    [Key(3)]
    public int timeout { get; set; } = 30000;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
