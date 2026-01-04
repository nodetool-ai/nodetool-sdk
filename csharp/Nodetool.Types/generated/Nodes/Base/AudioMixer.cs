using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AudioMixer
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef track1 { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public Nodetool.Types.Core.AudioRef track2 { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(2)]
    public Nodetool.Types.Core.AudioRef track3 { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(3)]
    public Nodetool.Types.Core.AudioRef track4 { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(4)]
    public Nodetool.Types.Core.AudioRef track5 { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(5)]
    public double volume1 { get; set; } = 1.0;
    [Key(6)]
    public double volume2 { get; set; } = 1.0;
    [Key(7)]
    public double volume3 { get; set; } = 1.0;
    [Key(8)]
    public double volume4 { get; set; } = 1.0;
    [Key(9)]
    public double volume5 { get; set; } = 1.0;

    public Nodetool.Types.Core.AudioRef Process()
    {
        return default(Nodetool.Types.Core.AudioRef);
    }
}
