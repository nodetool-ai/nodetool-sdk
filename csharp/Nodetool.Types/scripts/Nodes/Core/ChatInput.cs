using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class ChatInput
{
    [Key(0)]
    public object value { get; set; } = new List<object>();
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    [MessagePackObject]
    public class ChatInputOutput
    {
        [Key(0)]
        public object history { get; set; }
        [Key(1)]
        public string text { get; set; }
        [Key(2)]
        public Nodetool.Types.ImageRef image { get; set; }
        [Key(3)]
        public Nodetool.Types.AudioRef audio { get; set; }
        [Key(4)]
        public Nodetool.Types.VideoRef video { get; set; }
        [Key(5)]
        public Nodetool.Types.DocumentRef document { get; set; }
        [Key(6)]
        public object tools { get; set; }
    }

    public ChatInputOutput Process()
    {
        // Implementation would be generated based on node logic
        return new ChatInputOutput();
    }
}
