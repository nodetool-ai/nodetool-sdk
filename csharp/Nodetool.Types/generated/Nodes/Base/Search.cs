using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Search
{
    [Key(0)]
    public Nodetool.Types.Core.FaissIndex index { get; set; } = new Nodetool.Types.Core.FaissIndex();
    [Key(1)]
    public int k { get; set; } = 5;
    [Key(2)]
    public int? nprobe { get; set; } = null;
    [Key(3)]
    public Nodetool.Types.Core.NPArray query { get; set; } = new Nodetool.Types.Core.NPArray();

    [MessagePackObject]
    public class SearchOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.NPArray distances { get; set; }
        [Key(1)]
        public Nodetool.Types.Core.NPArray indices { get; set; }
    }

    public SearchOutput Process()
    {
        return new SearchOutput();
    }
}
