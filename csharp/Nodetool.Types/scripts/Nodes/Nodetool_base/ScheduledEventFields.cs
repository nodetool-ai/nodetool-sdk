using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class ScheduledEventFields
{
    [Key(0)]
    public Nodetool.Types.CalendlyEvent event { get; set; } = new Nodetool.Types.CalendlyEvent();

    [MessagePackObject]
    public class ScheduledEventFieldsOutput
    {
        [Key(0)]
        public string uri { get; set; }
        [Key(1)]
        public string name { get; set; }
        [Key(2)]
        public Nodetool.Types.Datetime start_time { get; set; }
        [Key(3)]
        public Nodetool.Types.Datetime end_time { get; set; }
        [Key(4)]
        public string location { get; set; }
    }

    public ScheduledEventFieldsOutput Process()
    {
        // Implementation would be generated based on node logic
        return new ScheduledEventFieldsOutput();
    }
}
