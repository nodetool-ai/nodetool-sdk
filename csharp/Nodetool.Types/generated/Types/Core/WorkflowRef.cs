using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class WorkflowRef
{
    [Key(0)]
    public object type { get; set; } = "workflow";
    [Key(1)]
    public string id { get; set; } = "";
}
