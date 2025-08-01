using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class FileNameMatch
{
    [Key(0)]
    public string filename { get; set; } = "";
    [Key(1)]
    public string pattern { get; set; } = "*";
    [Key(2)]
    public bool case_sensitive { get; set; } = true;

    public bool Process()
    {
        // Implementation would be generated based on node logic
        return default(bool);
    }
}
