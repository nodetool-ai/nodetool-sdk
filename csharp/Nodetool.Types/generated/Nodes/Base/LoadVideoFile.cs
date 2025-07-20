using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LoadVideoFile
{
    [Key(0)]
    public string path { get; set; } = "";

    public Nodetool.Types.VideoRef Process()
    {
        return default(Nodetool.Types.VideoRef);
    }
}
