using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class FaissIndex
{
    [Key(0)]
    public object index { get; set; } = null;
    [Key(1)]
    public object type { get; set; } = @"faiss_index";
}
