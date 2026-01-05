using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class StructuredOutputGenerator
{
    [Key(0)]
    public string context { get; set; } = @"";
    [Key(1)]
    public int context_window { get; set; } = 4096;
    [Key(2)]
    public string instructions { get; set; } = @"";
    [Key(3)]
    public int max_tokens { get; set; } = 4096;
    [Key(4)]
    public Nodetool.Types.Core.LanguageModel model { get; set; } = new Nodetool.Types.Core.LanguageModel();
    [Key(5)]
    public string system_prompt { get; set; } = @"
You are a structured data generator focused on JSON outputs.

Goal
- Produce a high-quality JSON object that matches <JSON_SCHEMA> using the guidance in <INSTRUCTIONS> and any supplemental <CONTEXT>.

Output format (MANDATORY)
- Output exactly ONE fenced code block labeled json containing ONLY the JSON object:

  ```json
  { ...single JSON object matching <JSON_SCHEMA>... }
  ```

- No additional prose before or after the block.

Generation rules
- Invent plausible, internally consistent values when not explicitly provided.
- Honor all constraints from <JSON_SCHEMA> (types, enums, ranges, formats).
- Prefer ISO 8601 for dates/times when applicable.
- Ensure numbers respect reasonable magnitudes and relationships described in <INSTRUCTIONS>.
- Avoid referencing external sources; rely solely on the provided guidance.

Validation
- Ensure the final JSON validates against <JSON_SCHEMA> exactly.
";

    public void Process()
    {
    }
}
