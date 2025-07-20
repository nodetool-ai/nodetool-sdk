using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class InferenceProviderTextGenerationModel
{
    [Key(0)]
    public object type { get; set; } = "inference_provider_text_generation_model";
    [Key(1)]
    public object provider { get; set; } = "InferenceProvider.hf_inference";
    [Key(2)]
    public string model_id { get; set; } = "";
}
