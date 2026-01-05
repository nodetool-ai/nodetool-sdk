using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LoadAudioFolder
{
    [Key(0)]
    public object extensions { get; set; } = new();
    [Key(1)]
    public string folder { get; set; } = @"";
    [Key(2)]
    public bool include_subdirectories { get; set; } = false;

    [MessagePackObject]
    public class LoadAudioFolderOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.AudioRef audio { get; set; }
        [Key(1)]
        public string path { get; set; }
    }

    public LoadAudioFolderOutput Process()
    {
        return new LoadAudioFolderOutput();
    }
}
