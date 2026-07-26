using Nodetool.SDK.VL;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public class PackageSurfaceTests
{
    [Fact]
    public void AdapterAssembly_DoesNotBroadlyImportManagedTypesAsVlNodes()
    {
        var forbiddenAttributes = new HashSet<string>(
            StringComparer.Ordinal)
        {
            "VL.Core.Import.ImportAsIsAttribute",
            "VL.Core.Import.ImportNamespaceAttribute",
            "VL.Core.Import.ImportTypeAttribute"
        };
        var importedAttributes = typeof(Initialization)
            .Assembly
            .GetCustomAttributesData()
            .Select(attribute => attribute.AttributeType.FullName)
            .Where(name => name != null && forbiddenAttributes.Contains(name))
            .ToArray();

        Assert.Empty(importedAttributes);
    }
}
