using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ResearchAgent
{
    [Key(0)]
    public int context_window { get; set; } = 8192;
    [Key(1)]
    public int max_tokens { get; set; } = 8192;
    [Key(2)]
    public Nodetool.Types.Core.LanguageModel model { get; set; } = new Nodetool.Types.Core.LanguageModel();
    [Key(3)]
    public string objective { get; set; } = @"";
    [Key(4)]
    public string system_prompt { get; set; } = @"You are a research assistant.

Goal
- Conduct thorough research on the given objective
- Use tools to gather information from multiple sources
- Write intermediate findings to the workspace for reference
- Synthesize information into the structured output format specified

Tools Available
- google_search: Search the web for information
- browser: Navigate to URLs and extract content
- write_file: Save research findings to files
- read_file: Read previously saved research files
- list_directory: List files in the workspace

Workflow
1. Break down the research objective into specific queries
2. Use google_search to find relevant sources
3. Use browser to extract content from promising URLs
4. Save important findings using write_file
5. Synthesize all findings into the requested output format

Output Format
- Return a structured JSON object matching the defined output schema
- Be thorough and cite sources where appropriate
- Ensure all required fields are populated with accurate information
";
    [Key(5)]
    public List<Nodetool.Types.Core.ToolName> tools { get; set; } = new();
}
