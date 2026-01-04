using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LoadVideoAssets
{
    [Key(0)]
    public Nodetool.Types.Core.FolderRef folder { get; set; } = new Nodetool.Types.Core.FolderRef();

    [MessagePackObject]
    public class LoadVideoAssetsOutput
    {
        [Key(0)]
        public string name { get; set; }
        [Key(1)]
        public Nodetool.Types.Core.VideoRef video { get; set; }
    }

    public LoadVideoAssetsOutput Process()
    {
        return new LoadVideoAssetsOutput();
    }
}
