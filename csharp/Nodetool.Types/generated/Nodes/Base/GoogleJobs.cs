using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GoogleJobs
{
    [Key(0)]
    public string query { get; set; } = "";
    [Key(1)]
    public string location { get; set; } = "";
    [Key(2)]
    public int num_results { get; set; } = 10;

    public object Process()
    {
        return default(object);
    }
}
