using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LoadTextAssets
{
    [Key(0)]
    public Nodetool.Types.Core.FolderRef folder { get; set; } = new Nodetool.Types.Core.FolderRef();

    [MessagePackObject]
    public class LoadTextAssetsOutput
    {
        [Key(0)]
        public string name { get; set; }
        [Key(1)]
        public Nodetool.Types.Core.TextRef text { get; set; }
    }

    public LoadTextAssetsOutput Process()
    {
        return new LoadTextAssetsOutput();
    }
}
