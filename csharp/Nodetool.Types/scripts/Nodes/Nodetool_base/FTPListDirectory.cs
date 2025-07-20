using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class FTPListDirectory
{
    [Key(0)]
    public string host { get; set; } = "";
    [Key(1)]
    public string username { get; set; } = "";
    [Key(2)]
    public string password { get; set; } = "";
    [Key(3)]
    public string directory { get; set; } = "";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
