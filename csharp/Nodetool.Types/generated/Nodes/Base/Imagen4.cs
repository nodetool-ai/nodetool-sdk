using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Imagen4
{
    [Key(0)]
    public object aspect_ratio { get; set; } = @"1:1";
    [Key(1)]
    public string negative_prompt { get; set; } = @"";
    [Key(2)]
    public string prompt { get; set; } = @"";
    [Key(3)]
    public int seed { get; set; } = 0;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
