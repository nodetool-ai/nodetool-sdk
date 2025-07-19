# Plan: Nodetool Node-Based VL/vvvv Integration (Refined)

## Overview

Create a new system that reads individual node metadata from Nodetool's API and generates corresponding VL/vvvv nodes with hierarchical namespace-based categories and trigger-based execution.

## Key Differences from Current System

- **Current**: Creates nodes from workflow definitions (`/api/workflows`)
- **New**: Creates nodes from individual node metadata (`/api/nodes/metadata`)
- **Current**: Uses HTTP POST for workflow execution
- **New**: Uses WebSocket via existing `WebSocketRunner` for individual node execution
- **Current**: Workflow-level operations
- **New**: Individual node execution with trigger-based processing
- **Current**: Single category "Nodetool Workflows"
- **New**: Hierarchical namespace-based categories (e.g., `Nodetool.HuggingFaceHub.AudioClassification`)

## Phase 1: Metadata Discovery and Analysis ✅

### Step 1.1: API Structure Analysis ✅

Based on actual `/api/nodes/metadata` response, each node has:

```json
{
  "title": "Audio Classification",
  "description": "Audio classification node using HuggingFace Inference API...",
  "namespace": "huggingface_hub",
  "node_type": "huggingface_hub.AudioClassification",
  "properties": [
    {
      "name": "model",
      "type": { "type": "inference_provider_audio_classification_model", ... },
      "default": { "provider": "fal-ai", "model_id": "..." },
      "title": "Model",
      "description": "The model to use for audio classification"
    }
  ],
  "outputs": [
    {
      "type": { "type": "dict", "type_args": [...] },
      "name": "output",
      "stream": false
    }
  ],
  "is_streaming": false,
  "is_dynamic": false
}
```

### Step 1.2: Node Classification and Categories ✅

- **Namespaces**: `huggingface_hub`, `nodetool.image`, `nodetool.audio`, `nodetool.input`, etc.
- **VL Categories**: Hierarchical structure based on namespaces:
  - `huggingface_hub` → `Nodetool.HuggingFaceHub`
  - `nodetool.image` → `Nodetool.Image`
  - `nodetool.audio` → `Nodetool.Audio`
- **Execution Types**: Regular nodes, streaming nodes (`is_streaming: true`)
- **Dynamic Nodes**: Nodes with dynamic properties (`is_dynamic: true`)

## Phase 2: Core Architecture Design (Refined)

### Step 2.1: Class Structure

```
VL.NodetoolNodes/
├── Core/
│   ├── NodetoolNodeBase.cs           # Abstract base implementation of IVLNode
│   ├── NodetoolNodeDescription.cs    # Implements IVLNodeDescription
│   ├── NodetoolPinDescription.cs     # Implements IVLPinDescription
│   └── Initialization.cs             # Assembly initializer
├── Discovery/
│   ├── NodeMetadataService.cs        # Fetches from /api/nodes/metadata
│   ├── NodeDefinition.cs             # Represents single node metadata
│   └── NodetoolNodeFactory.cs        # Creates VL nodes from metadata
├── Execution/
│   ├── WebSocketExecutor.cs          # Uses existing WebSocketRunner pattern
│   ├── NodeExecutionContext.cs       # Manages individual node execution
│   └── NodeExecutionRequest.cs       # Request format for single node
├── TypeSystem/
│   ├── NodetoolTypeMapper.cs         # Maps API types to VL types
│   ├── DataConverter.cs              # JSON serialization/deserialization
│   └── DataTypeDefinitions.cs        # From data_types.tsx reference
└── Utils/
    ├── NameSanitizer.cs              # VL-compatible name generation
    ├── CategoryMapper.cs             # Namespace to VL category mapping
    └── ConfigurationManager.cs       # API endpoints and settings
```

### Step 2.2: Integration Points

- [ ] Assembly initialization with existing logging pattern
- [ ] Node factory registration following VL.NodetoolUtils pattern
- [ ] Hierarchical category organization
- [ ] Trigger-based execution (no automatic updates)

## Phase 3: Implementation Plan (Priority Order)

### Step 3.1: Foundation (MVP - Priority 1) ✅ COMPLETED

1. **Project Setup** ✅

   - [x] Create `VL.NodetoolNodes.csproj` with clean dependencies
   - [x] Copy logging setup from existing VL.NodetoolUtils
   - [x] Set up basic project structure

2. **NodeMetadataService.cs** ✅

   - [x] Fetch from `http://localhost:8000/api/nodes/metadata`
   - [x] Parse JSON response into NodeDefinition objects
   - [x] Cache metadata with error handling
   - [x] Log nodes found (count by namespace)

3. **NodeDefinition.cs** ✅

   - [x] Model the exact API response structure
   - [x] Handle properties, outputs, defaults
   - [x] Store namespace, node_type, streaming flags

4. **NodetoolTypeMapper.cs** ✅
   - [x] Map API types to VL types based on data_types.tsx
   - [x] Handle: `str`→`string`, `int`→`int`, `float`→`float`, `bool`→`bool`
   - [x] Handle complex types: `image`, `audio`, `dict`, `list`
   - [x] Provide appropriate default values

### Step 3.2: VL Integration (Priority 2) ✅ COMPLETED

1. **CategoryMapper.cs** ✅

   - [x] Convert namespace to hierarchical VL categories
   - [x] `huggingface_hub` → `Nodetool.HuggingFaceHub`
   - [x] Handle special cases and sanitization

2. **NodetoolNodeDescription.cs** ✅

   - [x] Implement IVLNodeDescription
   - [x] Generate pins from properties and outputs
   - [x] Add standard trigger input pin
   - [x] Add standard status/error output pins

3. **NodetoolNodeBase.cs** ✅

   - [x] Implement IVLNode interface
   - [x] Handle trigger detection (rising edge)
   - [x] Manage pin values and updates
   - [x] Execute via simulated execution (WebSocket pending)

4. **NodetoolNodeFactory.cs** ✅
   - [x] Generate node descriptions from metadata
   - [x] Register with VL factory system
   - [x] Handle dynamic node creation

### Step 3.3: Execution Engine (Priority 3)

1. **WebSocketExecutor.cs**

   - [ ] Establish WebSocket connection to `ws://localhost:8000/ws`
   - [ ] Send individual node execution requests
   - [ ] Handle responses and errors
   - [ ] Connection lifecycle management

2. **NodeExecutionRequest.cs**

   - [ ] Format single node execution for WebSocket
   - [ ] Include node_type, properties, and input values
   - [ ] Handle streaming vs non-streaming execution

3. **NodeExecutionContext.cs**
   - [ ] Track execution state per node instance
   - [ ] Handle async execution and results
   - [ ] Manage execution queues if needed

## Phase 4: Refined Questions Based on Analysis

### Technical Questions (Updated)

1. **WebSocket Message Format**: How should individual nodes be executed via WebSocket?

   - Can we adapt existing `WebSocketRunner` command structure?
   - What's the message format for single node vs workflow execution?

2. **Node Execution Model**:

   - Do we create mini-workflows with single nodes?
   - How do we handle node dependencies when executing individually?

3. **Data Serialization**:
   - How do we serialize VL data types to JSON for WebSocket?
   - How do we handle complex types like images/audio in pin values?

### Design Decisions (Confirmed)

1. **Category Structure**: ✅ Hierarchical based on namespaces
2. **Execution Model**: ✅ Trigger-based, no automatic updates
3. **Pin Structure**: ✅ Add trigger input + status/error outputs to all nodes
4. **Logging**: ✅ Use existing pattern from VL.NodetoolUtils

## Phase 5: Implementation Priorities (Refined)

### MVP Deliverables

- [ ] Fetch and log node metadata by namespace
- [ ] Create VL node descriptions with correct pins
- [ ] Implement basic trigger-based execution
- [ ] Hierarchical category structure
- [ ] Basic WebSocket communication

### Testing Strategy

- [ ] Start with simple nodes (string/number operations)
- [ ] Test complex types (images) separately
- [ ] Verify VL integration with node browser
- [ ] Test WebSocket connection and execution

### Success Criteria

- [ ] Nodes appear in VL node browser under correct categories
- [ ] Trigger input causes node execution via WebSocket
- [ ] Results appear on output pins
- [ ] Error handling works correctly
- [ ] Logging provides useful debugging information

---

## Next Immediate Steps

1. **Create project structure** with clean dependencies
2. **Implement NodeMetadataService** to fetch and log nodes
3. **Build type mapping system** based on API response types
4. **Create basic VL node descriptions** with hierarchical categories
5. **Test with simple nodes first** before complex execution

## Questions for Clarification

1. **WebSocket Protocol**: Should we extend existing `WebSocketRunner` commands or create new message format?
2. **Node Dependencies**: How should we handle nodes that depend on others when executing individually?
3. **Data Persistence**: Should node results be cached or re-executed each trigger?
4. **Error Recovery**: What should happen when WebSocket connection fails during execution?
