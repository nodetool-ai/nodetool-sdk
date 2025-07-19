# VL Writing Nodes Reference

## 🎯 **Key Discovery: How VL Shows Documentation**

VL uses **XML documentation comments** for node tooltips and help, NOT `IVLNodeDescription.Summary/Remarks` properties!

```csharp
///<summary>Multiplies input by two</summary>
///<remarks>Some additional remarks</remarks>
///<param name="a">The A Parameter</param>
///<returns>Returns 2 times a</returns>
public static int HTMLDocuTest(int a)
{
    return a*2;
}
```

### **Documentation Elements**:

- `<summary>`: One-liner info about the node (shows in tooltips)
- `<remarks>`: Additional remarks, usage instructions, warnings (multi-line)
- `<param name="name">`: Short info for each input pin
- `<returns>`: Short info about the result/output

## 📋 **Essential VL C# Integration Rules**

### **Project Requirements**

```xml
<!-- CRITICAL: Must have this property in .csproj -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>

<!-- CRITICAL: Must have this assembly attribute -->
[assembly:ImportAsIs]
```

### **Pin Naming**

- **C# Parameter**: `firstInput` → **VL Pin**: `First Input` (camelCase separation)
- **Default return**: Called `Output` in VL
- **Out parameters**: Become additional output pins

### **Pin Defaults**

```csharp
public static float Defaults(float firstInput = 44f, float secondInput = 0.44f)
{
    return firstInput + secondInput;
}
```

### **Multiple Outputs**

```csharp
public static void MultipleOutputs(float firstInput, float secondInput,
    out float added, out float multiplied)
{
    added = firstInput + secondInput;
    multiplied = firstInput * secondInput;
}
```

## 🏗️ **Node Types**

### **Static Methods** (Functional Nodes)

```csharp
public static float Add(float a, float b)
{
    return a + b;
}
```

- Hotswap works perfectly
- No state issues
- Recommended for simple operations

### **Process Nodes** (Stateful)

```csharp
[ProcessNode]
public class Counter
{
    private int _value;

    public int Update(int increment)
    {
        return _value += increment;
    }
}
```

- Use `[ProcessNode]` attribute
- One instance per node in VL
- Proper create/dispose lifecycle
- State survives between calls

### **Data Types** (Classes/Structs)

```csharp
public class MyDataType
{
    public MyDataType(float x) { FX = x; }  // → Create node

    public float AddValue(float value)       // → Member operation
    {
        FX += value;
        return FX;
    }

    public float Value { get; set; }         // → Getter + Setter nodes
}
```

## 🔧 **Advanced Features**

### **Generics**

```csharp
public static string Generic<T>(T input)
{
    return input.ToString();
}
```

### **Spreads/Collections**

```csharp
public static IEnumerable<float> ReverseSequence(IEnumerable<float> input)
{
    return input.Reverse();
}
```

- **C# IEnumerable<>** → **VL Sequence<>**

### **Enums**

```csharp
public enum DemoEnum { Foo, Bar };

public static string StaticEnumDemo(DemoEnum e)
{
    return e.ToString();
}
```

### **Events/Observables**

```csharp
public class MyClass
{
    public event EventHandler OnValueChanged;
    public event EventHandler<MyEventArgs<float>> OnValueExceeded;
}
```

- Events become Observable<EventPattern<>> nodes
- Conform to .NET Core Event Pattern

## ⚠️ **Critical Constraints**

### **Hotswap Limitations**

- **Static methods**: Perfect hotswap
- **Process nodes**: Managed disposal on code changes
- **Dynamic instances**: All state lost on save!
- **Unmanaged resources**: Require VL restart (dangerous!)

### **Ref Parameters**

```csharp
public static int RefParams(ref int firstInput)
{
    // NEVER assign to ref parameter - only read!
    return firstInput + 4444;
}
```

### **Namespaces**

```csharp
namespace MyCompany.Audio.Effects  // → VL Category: MyCompany.Audio.Effects
{
    public static void Reverb() { }
}
```

- **C# Namespace** → **VL Category** (hierarchical)
- Use `[ImportAsIs]` to strip namespace prefixes

## 🎯 **For Our NodeTool Integration**

### **Why Our Descriptions Don't Show**

We're setting `IVLNodeDescription.Summary/Remarks` but VL expects **XML documentation**:

```csharp
// ❌ This doesn't create tooltips:
Summary = nodeDefinition.Description;
Remarks = "Some info";

// ✅ This creates tooltips:
/// <summary>Resize an image to specified dimensions</summary>
/// <remarks>Supports PNG, JPEG, WebP formats</remarks>
/// <param name="image">Input image to resize</param>
/// <param name="width">Target width in pixels</param>
/// <returns>Resized image</returns>
public static string ResizeImage(string image, int width) { ... }
```

### **Solution Options**

**Option 1: Dynamic Code Generation**

- Generate C# static methods with XML docs from NodeTool metadata
- Compile at runtime
- Full VL tooltip integration

**Option 2: Manual XML Documentation**

- Add XML docs to our `NodetoolNodeDescription` class
- Enable `GenerateDocumentationFile` in project
- Limited since we can't document dynamically generated nodes

**Option 3: VL Documentation API**

- Research if VL has APIs to set documentation programmatically
- Might be possible through `IVLNodeDescription` extended interfaces

### **Next Steps**

1. **Research**: Check if VL has programmatic documentation APIs
2. **Enable XML Generation**: Ensure our project generates XML docs
3. **Consider Code Gen**: For full XML documentation support
4. **Test XML Approach**: Try adding XML docs to our base classes

## 📚 **References**

- Pin names: CamelCase → "Separated Words"
- Documentation: XML comments with `<summary>`, `<remarks>`, `<param>`, `<returns>`
- Project: Must have `GenerateDocumentationFile=true` and `[assembly:ImportAsIs]`
- Process nodes: Use `[ProcessNode]` attribute for stateful nodes
- Hotswap: Works for static methods, state loss for instances
