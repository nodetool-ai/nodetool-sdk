using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class JSONSplitter
{
    [Key(0)]
    public string document_id { get; set; } = "";
    [Key(1)]
    public string text { get; set; } = "";
    [Key(2)]
    public bool include_metadata { get; set; } = true;
    [Key(3)]
    public bool include_prev_next_rel { get; set; } = true;

    public object Process()
    {
        return default(object);
    }
}
