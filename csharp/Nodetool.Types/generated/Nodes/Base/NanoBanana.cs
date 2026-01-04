using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class NanoBanana
{
    [Key(0)]
    public object image_size { get; set; } = @"1:1";
    [Key(1)]
    public string prompt { get; set; } = @"";

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
