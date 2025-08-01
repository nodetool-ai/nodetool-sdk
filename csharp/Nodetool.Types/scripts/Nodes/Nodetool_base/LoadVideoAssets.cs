using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

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
        // Implementation would be generated based on node logic
        return new LoadVideoAssetsOutput();
    }
}
