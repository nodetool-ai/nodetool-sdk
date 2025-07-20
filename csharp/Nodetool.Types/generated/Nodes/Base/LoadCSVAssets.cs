using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Data;

[MessagePackObject]
public class LoadCSVAssets
{
    [Key(0)]
    public Nodetool.Types.FolderRef folder { get; set; } = new Nodetool.Types.FolderRef();

    [MessagePackObject]
    public class LoadCSVAssetsOutput
    {
        [Key(0)]
        public Nodetool.Types.DataframeRef dataframe { get; set; }
        [Key(1)]
        public string name { get; set; }
    }

    public LoadCSVAssetsOutput Process()
    {
        // Implementation would be generated based on node logic
        return new LoadCSVAssetsOutput();
    }
}
