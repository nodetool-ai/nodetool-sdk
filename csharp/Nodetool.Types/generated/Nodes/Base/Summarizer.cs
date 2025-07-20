using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Llms;

[MessagePackObject]
public class Summarizer
{
    [Key(0)]
    public string system_prompt { get; set; } = "
        You are an expert summarizer. Your task is to create clear, accurate, and concise summaries using Markdown for structuring. 
        Follow these guidelines:
        1. Identify and include only the most important information.
        2. Maintain factual accuracy - do not add or modify information.
        3. Use clear, direct language.
        4. Aim for approximately {self.max_tokens} tokens.
        ";
    [Key(1)]
    public Nodetool.Types.LanguageModel model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(2)]
    public string text { get; set; } = "";
    [Key(3)]
    public int max_tokens { get; set; } = 200;
    [Key(4)]
    public int context_window { get; set; } = 4096;

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
