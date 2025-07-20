using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class HTMLSplitter
{
    [Key(0)]
    public string document_id { get; set; } = "";
    [Key(1)]
    public string text { get; set; } = "";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
