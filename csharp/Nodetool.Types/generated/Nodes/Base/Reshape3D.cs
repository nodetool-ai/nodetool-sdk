using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Reshape3D
{
    [Key(0)]
    public int num_cols { get; set; } = 0;
    [Key(1)]
    public int num_depths { get; set; } = 0;
    [Key(2)]
    public int num_rows { get; set; } = 0;
    [Key(3)]
    public Nodetool.Types.Core.NPArray values { get; set; } = new Nodetool.Types.Core.NPArray();

    public Nodetool.Types.Core.NPArray Process()
    {
        return default(Nodetool.Types.Core.NPArray);
    }
}
