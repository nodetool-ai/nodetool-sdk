using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AudioToNumpy
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();

    [MessagePackObject]
    public class AudioToNumpyOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.NPArray array { get; set; }
        [Key(1)]
        public int channels { get; set; }
        [Key(2)]
        public int sample_rate { get; set; }
    }

    public AudioToNumpyOutput Process()
    {
        return new AudioToNumpyOutput();
    }
}
