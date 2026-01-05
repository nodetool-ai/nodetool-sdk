using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class TorchTensor
{
    [Key(0)]
    public string dtype { get; set; } = @"<i8";
    [Key(1)]
    public List<int> shape { get; set; }
    [Key(2)]
    public object type { get; set; } = @"torch_tensor";
    [Key(3)]
    public byte[]? value { get; set; } = null;
}
