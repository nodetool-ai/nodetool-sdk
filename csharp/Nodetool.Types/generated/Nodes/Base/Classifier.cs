using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Classifier
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public List<string> categories { get; set; } = new();
    [Key(2)]
    public int context_window { get; set; } = 4096;
    [Key(3)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(4)]
    public Nodetool.Types.Core.LanguageModel model { get; set; } = new Nodetool.Types.Core.LanguageModel();
    [Key(5)]
    public string system_prompt { get; set; } = @"
You are a precise classifier.

Goal
- Select exactly one category from the list provided by the user.

Output format (MANDATORY)
- Return ONLY a single JSON object with this exact schema and nothing else:
  {""category"": ""<one-of-the-allowed-categories>""}
- No prose, no Markdown, no code fences, no explanations, no extra keys.

Selection criteria
- Choose the single best category that captures the main intent of the text.
- If multiple categories seem plausible, pick the most probable one; do not return multiple.
- If none fit perfectly, choose the closest allowed category. If the list includes ""Other"" or ""Unknown"", prefer it when appropriate.
- Be robust to casing, punctuation, emojis, and minor typos. Handle negation correctly (e.g., ""not spam"" ≠ spam).
- Never invent categories that are not in the provided list.

Behavior
- Be deterministic for the same input.
- Do not ask clarifying questions; make the best choice with what's given.
";
    [Key(6)]
    public string text { get; set; } = @"";
}
