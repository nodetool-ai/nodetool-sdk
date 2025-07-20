using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class LoadBytesFile
{
    [Key(0)]
    public Nodetool.Types.FilePath path { get; set; } = new Nodetool.Types.FilePath();

    public byte[] Process()
    {
        // Implementation would be generated based on node logic
        return default(byte[]);
    }
}
