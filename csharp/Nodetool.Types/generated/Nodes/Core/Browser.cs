using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Browser
{
    [Key(0)]
    public string url { get; set; } = "";
    [Key(1)]
    public int timeout { get; set; } = 20000;
    [Key(2)]
    public bool use_readability { get; set; } = true;

    [MessagePackObject]
    public class BrowserOutput
    {
        [Key(0)]
        public bool success { get; set; }
        [Key(1)]
        public string content { get; set; }
        [Key(2)]
        public object metadata { get; set; }
    }

    public BrowserOutput Process()
    {
        // Implementation would be generated based on node logic
        return new BrowserOutput();
    }
}
