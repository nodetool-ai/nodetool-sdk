using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LoadVideoAssets
{
    [Key(0)]
    public Nodetool.Types.FolderRef folder { get; set; } = new Nodetool.Types.FolderRef();

    [MessagePackObject]
    public class LoadVideoAssetsOutput
    {
        [Key(0)]
        public Nodetool.Types.VideoRef video { get; set; }
        [Key(1)]
        public string name { get; set; }
    }

    public LoadVideoAssetsOutput Process()
    {
        return new LoadVideoAssetsOutput();
    }
}
