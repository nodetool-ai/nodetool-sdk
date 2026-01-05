using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FetchPage
{
    [Key(0)]
    public string url { get; set; } = @"";
    [Key(1)]
    public int wait_time { get; set; } = 10;

    [MessagePackObject]
    public class FetchPageOutput
    {
        [Key(0)]
        public string error_message { get; set; }
        [Key(1)]
        public string html { get; set; }
        [Key(2)]
        public bool success { get; set; }
    }

    public FetchPageOutput Process()
    {
        return new FetchPageOutput();
    }
}
