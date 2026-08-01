using Nodetool.SDK.VL.Hde;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public sealed class HdeModelFamilyClassifierTests
{
    [Theory]
    [InlineData("language_model", "Language")]
    [InlineData("embedding_model", "Language")]
    [InlineData("hf.text_generation", "Language")]
    [InlineData("image_model", "Image")]
    [InlineData("hf.flux", "Image")]
    [InlineData("hf.depth_estimation", "Image")]
    [InlineData("asr_model", "Audio")]
    [InlineData("hf.text_to_audio", "Audio")]
    [InlineData("video_model", "Video3D")]
    [InlineData("mesh_model", "Video3D")]
    [InlineData("custom_model", "Other")]
    public void CompatibilityMapsToBroadEditorFamily(
        string compatibility,
        string expected)
        => Assert.Equal(
            expected,
            HdeModelFamilyClassifier.Classify(compatibility).ToString());
}
