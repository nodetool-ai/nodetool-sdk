using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class InferenceProviderTextToImageModel
{
    [Key(0)]
    public string model_id { get; set; } = @"";
    [Key(1)]
    public object provider { get; set; } = @"hf-inference";
    [Key(2)]
    public object type { get; set; } = @"inference_provider_text_to_image_model";
}
