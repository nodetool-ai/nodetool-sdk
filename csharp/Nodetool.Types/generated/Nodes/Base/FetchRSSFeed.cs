using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FetchRSSFeed
{
    [Key(0)]
    public string url { get; set; } = @"";

    [MessagePackObject]
    public class FetchRSSFeedOutput
    {
        [Key(0)]
        public string author { get; set; }
        [Key(1)]
        public string link { get; set; }
        [Key(2)]
        public Nodetool.Types.Core.Datetime published { get; set; }
        [Key(3)]
        public string summary { get; set; }
        [Key(4)]
        public string title { get; set; }
    }

    public FetchRSSFeedOutput Process()
    {
        return new FetchRSSFeedOutput();
    }
}
