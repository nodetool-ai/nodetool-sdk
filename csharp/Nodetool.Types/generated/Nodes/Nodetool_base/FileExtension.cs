using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class FileExtension
{
    [Key(0)]
    public Nodetool.Types.FilePath path { get; set; } = new Nodetool.Types.FilePath();

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
