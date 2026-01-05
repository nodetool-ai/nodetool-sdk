using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LoadAudioAssets
{
    [Key(0)]
    public Nodetool.Types.Core.FolderRef folder { get; set; } = new Nodetool.Types.Core.FolderRef();

    [MessagePackObject]
    public class LoadAudioAssetsOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.AudioRef audio { get; set; }
        [Key(1)]
        public string name { get; set; }
    }

    public LoadAudioAssetsOutput Process()
    {
        return new LoadAudioAssetsOutput();
    }
}
