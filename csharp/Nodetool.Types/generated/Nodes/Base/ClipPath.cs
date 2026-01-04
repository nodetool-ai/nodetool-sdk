using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ClipPath
{
    [Key(0)]
    public Nodetool.Types.Core.SVGElement clip_content { get; set; } = null;
    [Key(1)]
    public Nodetool.Types.Core.SVGElement content { get; set; } = null;

    public Nodetool.Types.Core.SVGElement Process()
    {
        return default(Nodetool.Types.Core.SVGElement);
    }
}
