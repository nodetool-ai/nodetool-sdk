using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LoadImageAssets
{
    [Key(0)]
    public Nodetool.Types.FolderRef folder { get; set; } = new Nodetool.Types.FolderRef();

    [MessagePackObject]
    public class LoadImageAssetsOutput
    {
        [Key(0)]
        public Nodetool.Types.ImageRef image { get; set; }
        [Key(1)]
        public string name { get; set; }
    }

    public LoadImageAssetsOutput Process()
    {
        return new LoadImageAssetsOutput();
    }
}
