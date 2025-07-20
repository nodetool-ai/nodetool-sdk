using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class ListTarFile
{
    [Key(0)]
    public Nodetool.Types.FilePath tar_path { get; set; } = new Nodetool.Types.FilePath();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
