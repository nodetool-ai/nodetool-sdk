# 🚀 **Add Workflow Execution Plan** _(REFINED)_

## 📋 **Overview**

Add workflow execution capabilities to VL.NodetoolNodes, enabling users to execute complete Nodetool workflows from within VL. **Key insight**: Workflows are pre-composed graphs that Nodetool executes internally - much simpler than individual node orchestration!

## 🔍 **Analysis of bak_nodetool_workflows**

### **✅ Key Patterns from Existing Implementation**:

1. **📡 Actual API Pattern** _(corrected)_:

   - `GET /api/workflows/` - Returns workflow list with schemas
   - `POST /api/workflows/{id}/run` - Execute workflow with params
   - Supports both streaming (`stream=true`) and non-streaming execution
   - Returns final results as JSON dictionary for non-streaming

2. **🗂️ Schema-Based Type System**:

   - `input_schema` and `output_schema` define workflow interface
   - Schema properties map directly to VL pins (string, number, integer, boolean, object)
   - Object types become JSON strings in VL

3. **📡 Actual API Structure** _(from nodetool-core/api/workflow.py)_:

   ```csharp
   // Workflow execution request
   public class RunWorkflowRequest
   {
       public Dictionary<string, object> Params { get; set; } = new();
   }

   // API calls
   GET /api/workflows/                    // List workflows with schemas
   POST /api/workflows/{id}/run          // Execute workflow
   {
       "params": {
           "input_param1": "value1",
           "input_param2": 42
       }
   }

   // Response (non-streaming)
   {
       "output_param1": "result_value",
       "output_param2": { "data": "..." }
   }
   ```

4. **📖 Workflow Data Models** _(from bak_nodetool_workflows)_:

   ```csharp
   public class NodetoolWorkflowInfo
   {
       public string Id { get; set; }
       public string Name { get; set; }
       public string Description { get; set; }
       public SchemaDefinition InputSchema { get; set; }
       public SchemaDefinition OutputSchema { get; set; }
   }
   ```

5. **🏗️ VL Integration Pattern**:
   - Each workflow = one VL node with trigger input + schema-based pins
   - Uses same `IVLNodeDescriptionFactory` pattern as our current nodes
   - Documentation from workflow description

## 🆕 **Simplified Architecture**

### **📁 Much Simpler Folder Structure**:

```
VL.NodetoolNodes/
├── Workflows/                          # 🆕 New folder
│   ├── WorkflowDefinition.cs           # Data models (reuse from bak)
│   ├── WorkflowMetadataService.cs      # API client for workflows
│   ├── WorkflowNodeFactory.cs          # VL factory for workflow nodes
│   ├── WorkflowNodeBase.cs             # VL workflow node implementation
│   └── WorkflowTypeMapper.cs           # Schema to VL type conversion
```

### **🔧 What We DON'T Need** _(vs. original plan)_:

- ❌ **Complex execution engine** - Nodetool handles this
- ❌ **Dependency resolution** - Pre-resolved in workflow graph
- ❌ **Node execution queue** - Single API call execution
- ❌ **Execution context** - Stateless workflow calls
- ❌ **Connection management** - Internal to Nodetool

### **✅ What We CAN Reuse from Our Current Codebase**:

1. **📡 HTTP Communication**:

   - `NodeMetadataService.cs` patterns for API calls
   - Error handling and async patterns
   - HTTP client setup

2. **🏗️ VL Integration Architecture**:

   - `NodetoolNodeFactory.cs` patterns
   - `bc.Pin()` and `bc.Node()` documentation system
   - Factory registration in `Initialization.cs`

3. **🗂️ Type System Foundation**:

   - Adapt `NodetoolTypeMapper.cs` for schema properties
   - JSON parsing utilities
   - Default value handling

4. **📖 Documentation System**:
   - `BuildPinRemarks()` patterns for schema properties
   - Rich tooltip generation with type info
   - Category mapping

## 📊 **Implementation Phases** _(Simplified)_

### **Phase 1: Copy & Adapt Data Models** _(2-3 hours)_

**Goal**: Copy proven data models from bak_nodetool_workflows and adapt for our codebase

**Tasks**:

1. **Copy Core Data Models**:

   ```csharp
   // From bak_nodetool_workflows/NodetoolWorkflow.cs
   public class SchemaPropertyDefinition { ... }
   public class SchemaDefinition { ... }
   public class NodetoolWorkflowInfo { ... }
   ```

2. **Adapt WorkflowTypeMapper**:
   ```csharp
   // Based on bak_nodetool_workflows/NodetoolTypeConverter.cs
   public static (Type? VLType, object? DefaultValue) MapSchemaProperty(
       SchemaPropertyDefinition schemaProp, string propertyName)
   {
       // string, number, integer, boolean, object → VL types
       // object types become JSON strings
   }
   ```

**Deliverables**:

- ✅ `Workflows/WorkflowDefinition.cs`
- ✅ `Workflows/WorkflowTypeMapper.cs`

### **Phase 2: API Service** _(3-4 hours)_

**Goal**: Create workflow discovery service using our proven HTTP patterns

**Tasks**:

1. **WorkflowMetadataService** _(reuse NodeMetadataService patterns)_:

   ```csharp
   public class WorkflowMetadataService
   {
       // GET /api/workflows/?cursor=&limit=200
       public async Task<List<NodetoolWorkflowInfo>> FetchWorkflowsAsync()

       // Uses same HTTP client, error handling, logging patterns
   }
   ```

2. **Reuse Existing Infrastructure**:
   - Same HTTP client setup from `NodeMetadataService`
   - Same error handling and logging patterns
   - Same async/await and cancellation patterns

**Deliverables**:

- ✅ `Workflows/WorkflowMetadataService.cs`
- ✅ API integration tested and working

### **Phase 3: VL Integration** _(4-6 hours)_

**Goal**: Create VL workflow nodes using our proven factory system

**Tasks**:

1. **WorkflowNodeFactory** _(reuse NodetoolNodeFactory patterns)_:

   ```csharp
   public class WorkflowNodeFactory : IVLNodeDescriptionFactory
   {
       // Each workflow becomes a VL node
       // Use bc.Pin() and bc.Node() with documentation
       // Trigger input + schema-based inputs/outputs
   }
   ```

2. **WorkflowNodeBase** _(simple execution node)_:

   ```csharp
   public class WorkflowNodeBase : IVLNode
   {
       // Trigger-based execution
       // POST to /api/workflows/{id}/run with JSON params
       // Parse response dictionary and update output pins
   }
   ```

3. **Documentation Integration**:

   ```csharp
   // Reuse our proven documentation system
   var summary = workflow.Description ?? workflow.Name;
   var remarks = BuildWorkflowRemarks(workflow);

   return bc.Node(
       inputs: workflowInputPins,
       outputs: workflowOutputPins,
       newNode: ibc => new WorkflowNodeBase(ibc.NodeContext, workflow),
       summary: summary,
       remarks: remarks
   );
   ```

**Deliverables**:

- ✅ Workflow nodes appear in VL node browser
- ✅ Rich documentation and tooltips
- ✅ Schema-based input/output pins

### **Phase 4: Integration & Testing** _(2-3 hours)_

**Goal**: Integrate with existing system and test end-to-end

**Tasks**:

1. **Factory Registration**:

   - Extend `Initialization.cs` to register workflow factory
   - Create "Nodetool Workflows" category

2. **End-to-End Testing**:
   - Fetch workflows from API
   - Create VL nodes with proper pins
   - Execute workflows and verify results
   - Test error handling and edge cases

**Deliverables**:

- ✅ Workflows integrated with existing node system
- ✅ Working workflow execution from VL
- ✅ Proper error handling and status reporting

## 🔧 **Architecture Benefits**

### **🔄 Maximum Code Reuse** _(95%+ reuse)_:

- **API Communication**: Same HTTP client, error handling, async patterns from `NodeMetadataService`
- **VL Integration**: Same factory pattern, documentation system from `NodetoolNodeFactory`
- **Type Mapping**: Adapt existing `NodetoolTypeMapper` for schema properties
- **Initialization**: Same factory registration pattern in `Initialization.cs`

### **📈 Proven Patterns**:

- **Schema-to-VL mapping** already validated in bak_nodetool_workflows
- **Single API call execution** much simpler than complex orchestration
- **Trigger-based workflow nodes** follow same pattern as individual nodes

### **🧪 Low Risk Implementation**:

- **Copy proven code** from bak_nodetool_workflows
- **Adapt existing patterns** rather than inventing new ones
- **Incremental integration** with existing working system

## 🎯 **Success Criteria** _(Simplified)_

1. **✅ Discovery**: Fetch workflows from `/api/workflows` with schema info
2. **✅ VL Integration**: Each workflow becomes a VL node with schema-based pins
3. **✅ Documentation**: Rich tooltips using our proven `bc.Pin()` system
4. **✅ Execution**: POST to `/api/workflows/{id}/run` with proper params works
5. **✅ User Experience**: Workflows feel just like individual nodes in VL

## 🚀 **Next Steps** _(Quick Implementation)_

1. **Phase 1**: Copy data models from bak_nodetool_workflows
2. **Phase 2**: Create WorkflowMetadataService using proven patterns
3. **Phase 3**: Build VL factory and execution nodes
4. **Phase 4**: Integration and testing

**Key Insight**: Workflows are much simpler than individual nodes because Nodetool handles all the complex orchestration internally. We just need to expose the workflow interface as VL pins and make a single API call for execution.

This refined plan leverages proven patterns from both our current node system. The result should be a robust workflow system that feels native to VL users.
