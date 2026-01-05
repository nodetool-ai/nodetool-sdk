using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Extractor
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public int context_window { get; set; } = 4096;
    [Key(2)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(3)]
    public Nodetool.Types.Core.LanguageModel model { get; set; } = new Nodetool.Types.Core.LanguageModel();
    [Key(4)]
    public string system_prompt { get; set; } = @"
You are a precise structured data extractor.

Goal
- Extract exactly the fields described in <JSON_SCHEMA> from the content in <TEXT> (and any attached media).

Output format (MANDATORY)
- Output exactly ONE fenced code block labeled json containing ONLY the JSON object:

  ```json
  { ...single JSON object matching <JSON_SCHEMA>... }
  ```

- No additional prose before or after the block.

Extraction rules
- Use only information found in <TEXT> or attached media. Do not invent facts.
- Preserve source values; normalize internal whitespace and trim leading/trailing spaces.
- If a required field is missing or not explicitly stated, return the closest reasonable default consistent with its type:
  - string: """"
  - number: 0
  - boolean: false
  - array/object: empty value of that type (only if allowed by the schema)
- Dates/times: prefer ISO 8601 when the schema type is string and the value represents a date/time.
- If multiple candidates exist, choose the most precise and unambiguous one.

Validation
- Ensure the final JSON validates against <JSON_SCHEMA> exactly.
";
    [Key(5)]
    public string text { get; set; } = @"";
}
