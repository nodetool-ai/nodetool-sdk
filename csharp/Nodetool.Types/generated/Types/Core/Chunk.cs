using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class Chunk
{
    [Key(0)]
    public string content { get; set; } = @"";
    [Key(1)]
    public Dictionary<string, object> content_metadata { get; set; } = new();
    [Key(2)]
    public object content_type { get; set; } = @"text";
    [Key(3)]
    public bool done { get; set; } = false;
    [Key(4)]
    public object node_id { get; set; } = null;
    [Key(5)]
    public object type { get; set; } = @"chunk";
}
