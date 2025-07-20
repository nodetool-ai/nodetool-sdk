using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class ModelFile
{
    [Key(0)]
    public string type { get; set; }
    [Key(1)]
    public string name { get; set; } = "";
}
