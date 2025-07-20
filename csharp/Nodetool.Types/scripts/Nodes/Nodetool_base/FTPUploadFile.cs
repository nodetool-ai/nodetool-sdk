using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class FTPUploadFile
{
    [Key(0)]
    public string host { get; set; } = "";
    [Key(1)]
    public string username { get; set; } = "";
    [Key(2)]
    public string password { get; set; } = "";
    [Key(3)]
    public string remote_path { get; set; } = "";
    [Key(4)]
    public Nodetool.Types.DocumentRef document { get; set; } = new Nodetool.Types.DocumentRef();

    public void Process()
    {
        // Implementation would be generated based on node logic
    }
}
