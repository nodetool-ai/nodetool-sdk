using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LoadTextAssets
{
    [Key(0)]
    public Nodetool.Types.FolderRef folder { get; set; } = new Nodetool.Types.FolderRef();

    [MessagePackObject]
    public class LoadTextAssetsOutput
    {
        [Key(0)]
        public Nodetool.Types.TextRef text { get; set; }
        [Key(1)]
        public string name { get; set; }
    }

    public LoadTextAssetsOutput Process()
    {
        return new LoadTextAssetsOutput();
    }
}
