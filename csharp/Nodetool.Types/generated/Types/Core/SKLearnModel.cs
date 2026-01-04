using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class SKLearnModel
{
    [Key(0)]
    public object model { get; set; } = null;
    [Key(1)]
    public object type { get; set; } = @"sklearn_model";
}
