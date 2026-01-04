using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CreateImage
{
    [Key(0)]
    public object background { get; set; } = @"auto";
    [Key(1)]
    public object model { get; set; } = @"gpt-image-1";
    [Key(2)]
    public string prompt { get; set; } = @"";
    [Key(3)]
    public object quality { get; set; } = @"high";
    [Key(4)]
    public object size { get; set; } = @"1024x1024";

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
