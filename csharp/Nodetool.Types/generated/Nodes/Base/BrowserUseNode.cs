using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class BrowserUseNode
{
    [Key(0)]
    public object model { get; set; } = @"gpt-4o";
    [Key(1)]
    public string task { get; set; } = @"";
    [Key(2)]
    public int timeout { get; set; } = 300;
    [Key(3)]
    public bool use_remote_browser { get; set; } = true;

    [MessagePackObject]
    public class BrowserUseNodeOutput
    {
        [Key(0)]
        public string error { get; set; }
        [Key(1)]
        public object result { get; set; }
        [Key(2)]
        public bool success { get; set; }
        [Key(3)]
        public string task { get; set; }
    }

    public BrowserUseNodeOutput Process()
    {
        return new BrowserUseNodeOutput();
    }
}
