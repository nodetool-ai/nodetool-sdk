using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib.Audio;

[MessagePackObject]
public class Phaser
{
    [Key(0)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(1)]
    public double rate_hz { get; set; } = 1.0;
    [Key(2)]
    public double depth { get; set; } = 0.5;
    [Key(3)]
    public double centre_frequency_hz { get; set; } = 1300.0;
    [Key(4)]
    public double feedback { get; set; } = 0.0;
    [Key(5)]
    public double mix { get; set; } = 0.5;

    public Nodetool.Types.AudioRef Process()
    {
        return default(Nodetool.Types.AudioRef);
    }
}
