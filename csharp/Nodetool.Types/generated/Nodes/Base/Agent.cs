using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Agent
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public int context_window { get; set; } = 4096;
    [Key(2)]
    public List<Nodetool.Types.Core.Message> history { get; set; } = new();
    [Key(3)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(4)]
    public int max_tokens { get; set; } = 8192;
    [Key(5)]
    public Nodetool.Types.Core.LanguageModel model { get; set; } = new Nodetool.Types.Core.LanguageModel();
    [Key(6)]
    public string prompt { get; set; } = @"";
    [Key(7)]
    public string system { get; set; } = @"You are a an AI agent. 

Behavior
- Understand the user's intent and the context of the task.
- Break down the task into smaller steps.
- Be precise, concise, and actionable.
- Use tools to accomplish your goal. 

Tool preambles
- Outline the next step(s) you will perform.
- After acting, summarize the outcome.

Rendering
- Use Markdown to display media assets.
- Display images, audio, and video assets using the appropriate Markdown.

File handling
- Inputs and outputs are files in the /workspace directory.
- Write outputs of code execution to the /workspace directory.
";
    [Key(8)]
    public object thread_id { get; set; } = null;
}
