using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExtractMetadata
{
    [Key(0)]
    public string html { get; set; } = "";

    public object Process()
    {
        return default(object);
    }
}
