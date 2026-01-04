using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LoadCSVAssets
{
    [Key(0)]
    public Nodetool.Types.Core.FolderRef folder { get; set; } = new Nodetool.Types.Core.FolderRef();

    [MessagePackObject]
    public class LoadCSVAssetsOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.DataframeRef dataframe { get; set; }
        [Key(1)]
        public string name { get; set; }
    }

    public LoadCSVAssetsOutput Process()
    {
        return new LoadCSVAssetsOutput();
    }
}
