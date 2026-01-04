using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RealtimeAgent
{
    [Key(0)]
    public Nodetool.Types.Core.Chunk chunk { get; set; } = new Nodetool.Types.Core.Chunk();
    [Key(1)]
    public object model { get; set; } = @"gpt-4o-mini-realtime-preview";
    [Key(2)]
    public double speed { get; set; } = 1.0;
    [Key(3)]
    public string system { get; set; } = @"
You are an AI assistant interacting in real-time. Follow these rules unless explicitly overridden by the user:

1. Respond promptly — minimize delay. If you do not yet have a complete answer, acknowledge the question and indicate what you are doing to find the answer.
2. Maintain correctness. Always aim for accuracy; if you’re uncertain, say so and optionally offer to verify.
3. Be concise but clear. Prioritize key information first, then supporting details if helpful.
4. Ask clarifying questions when needed. If the user’s request is ambiguous, request clarification rather than guessing.
5. Be consistent in terminology and definitions. Once you adopt a term or abbreviation, use it consistently in this conversation.
6. Respect politeness and neutrality. Do not use emotive language unless the conversation tone demands it.
7. Stay within safe and ethical bounds. Avoid disallowed content; follow OpenAI policies.
8. Adapt to the user’s style and level. If the user seems technical, use technical detail; if non-technical, explain with simpler language.
---
You are now active. Await the user’s request.
";
    [Key(4)]
    public double temperature { get; set; } = 0.8;
    [Key(5)]
    public object voice { get; set; } = @"alloy";

    [MessagePackObject]
    public class RealtimeAgentOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.AudioRef audio { get; set; }
        [Key(1)]
        public Nodetool.Types.Core.Chunk chunk { get; set; }
        [Key(2)]
        public string text { get; set; }
    }

    public RealtimeAgentOutput Process()
    {
        return new RealtimeAgentOutput();
    }
}
