using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class EmailFields
{
    [Key(0)]
    public Nodetool.Types.Email email { get; set; } = new Nodetool.Types.Email();

    [MessagePackObject]
    public class EmailFieldsOutput
    {
        [Key(0)]
        public string id { get; set; }
        [Key(1)]
        public string subject { get; set; }
        [Key(2)]
        public string sender { get; set; }
        [Key(3)]
        public Nodetool.Types.Datetime date { get; set; }
        [Key(4)]
        public string body { get; set; }
    }

    public EmailFieldsOutput Process()
    {
        // Implementation would be generated based on node logic
        return new EmailFieldsOutput();
    }
}
