using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class FetchPage
{
    [Key(0)]
    public string url { get; set; } = "";
    [Key(1)]
    public int wait_time { get; set; } = 10;

    [MessagePackObject]
    public class FetchPageOutput
    {
        [Key(0)]
        public string html { get; set; }
        [Key(1)]
        public bool success { get; set; }
        [Key(2)]
        public object error_message { get; set; }
    }

    public FetchPageOutput Process()
    {
        // Implementation would be generated based on node logic
        return new FetchPageOutput();
    }
}
