using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class LoadAudioAssets
{
    [Key(0)]
    public Nodetool.Types.FolderRef folder { get; set; } = new Nodetool.Types.FolderRef();

    [MessagePackObject]
    public class LoadAudioAssetsOutput
    {
        [Key(0)]
        public Nodetool.Types.AudioRef audio { get; set; }
        [Key(1)]
        public string name { get; set; }
    }

    public LoadAudioAssetsOutput Process()
    {
        // Implementation would be generated based on node logic
        return new LoadAudioAssetsOutput();
    }
}
