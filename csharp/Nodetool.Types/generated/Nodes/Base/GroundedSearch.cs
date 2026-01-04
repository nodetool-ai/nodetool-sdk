using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GroundedSearch
{
    [Key(0)]
    public object model { get; set; } = @"gemini-2.0-flash";
    [Key(1)]
    public string query { get; set; } = @"";

    [MessagePackObject]
    public class GroundedSearchOutput
    {
        [Key(0)]
        public object results { get; set; }
        [Key(1)]
        public object sources { get; set; }
    }

    public GroundedSearchOutput Process()
    {
        return new GroundedSearchOutput();
    }
}
