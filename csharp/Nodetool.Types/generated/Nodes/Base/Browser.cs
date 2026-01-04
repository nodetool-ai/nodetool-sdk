using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Browser
{
    [Key(0)]
    public int timeout { get; set; } = 20000;
    [Key(1)]
    public string url { get; set; } = @"";

    [MessagePackObject]
    public class BrowserOutput
    {
        [Key(0)]
        public string content { get; set; }
        [Key(1)]
        public object metadata { get; set; }
        [Key(2)]
        public bool success { get; set; }
    }

    public BrowserOutput Process()
    {
        return new BrowserOutput();
    }
}
