using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class DictionaryOutput
{
    [Key(0)]
    public object value { get; set; } = new Dictionary<string, object>();
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
