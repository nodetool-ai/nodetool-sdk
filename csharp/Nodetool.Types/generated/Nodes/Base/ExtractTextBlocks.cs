using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExtractTextBlocks
{
    [Key(0)]
    public int end_page { get; set; } = -1;
    [Key(1)]
    public Nodetool.Types.Core.DocumentRef pdf { get; set; } = new Nodetool.Types.Core.DocumentRef();
    [Key(2)]
    public int start_page { get; set; } = 0;

    public object Process()
    {
        return default(object);
    }
}
