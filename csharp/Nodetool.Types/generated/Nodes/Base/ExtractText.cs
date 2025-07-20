using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExtractText
{
    [Key(0)]
    public Nodetool.Types.DocumentRef pdf { get; set; } = new Nodetool.Types.DocumentRef();
    [Key(1)]
    public int start_page { get; set; } = 0;
    [Key(2)]
    public int end_page { get; set; } = -1;

    public string Process()
    {
        return default(string);
    }
}
