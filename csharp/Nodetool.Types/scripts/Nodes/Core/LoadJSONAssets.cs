using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class LoadJSONAssets
{
    [Key(0)]
    public Nodetool.Types.FolderRef folder { get; set; } = new Nodetool.Types.FolderRef();

    [MessagePackObject]
    public class LoadJSONAssetsOutput
    {
        [Key(0)]
        public object json { get; set; }
        [Key(1)]
        public string name { get; set; }
    }

    public LoadJSONAssetsOutput Process()
    {
        // Implementation would be generated based on node logic
        return new LoadJSONAssetsOutput();
    }
}
