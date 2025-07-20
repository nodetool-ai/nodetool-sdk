using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class GmailSearch
{
    [Key(0)]
    public string from_address { get; set; } = "";
    [Key(1)]
    public string to_address { get; set; } = "";
    [Key(2)]
    public string subject { get; set; } = "";
    [Key(3)]
    public string body { get; set; } = "";
    [Key(4)]
    public object date_filter { get; set; }
    [Key(5)]
    public string keywords { get; set; } = "";
    [Key(6)]
    public object folder { get; set; }
    [Key(7)]
    public string text { get; set; } = "";
    [Key(8)]
    public int max_results { get; set; } = 50;

    [MessagePackObject]
    public class GmailSearchOutput
    {
        [Key(0)]
        public Nodetool.Types.Email email { get; set; }
        [Key(1)]
        public string message_id { get; set; }
    }

    public GmailSearchOutput Process()
    {
        // Implementation would be generated based on node logic
        return new GmailSearchOutput();
    }
}
