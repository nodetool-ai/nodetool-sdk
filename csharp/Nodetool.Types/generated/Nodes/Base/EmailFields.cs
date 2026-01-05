using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class EmailFields
{
    [Key(0)]
    public Nodetool.Types.Core.Email email { get; set; } = new Nodetool.Types.Core.Email();

    [MessagePackObject]
    public class EmailFieldsOutput
    {
        [Key(0)]
        public string body { get; set; }
        [Key(1)]
        public Nodetool.Types.Core.Datetime date { get; set; }
        [Key(2)]
        public string id { get; set; }
        [Key(3)]
        public string sender { get; set; }
        [Key(4)]
        public string subject { get; set; }
    }

    public EmailFieldsOutput Process()
    {
        return new EmailFieldsOutput();
    }
}
