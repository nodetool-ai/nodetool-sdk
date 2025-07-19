using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Core.Diagnostics;
using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.VL.Nodes
{
    /// <summary>
    /// VL node description for individual Nodetool nodes
    /// </summary>
    public class NodeDescription : IVLNodeDescription
    {
        private readonly NodeMetadataResponse _nodeMetadata;
        private readonly IVLNodeDescriptionFactory _factory;

        public NodeDescription(NodeMetadataResponse nodeMetadata, string vlNodeName, string category, IVLNodeDescriptionFactory factory)
        {
            _nodeMetadata = nodeMetadata ?? throw new ArgumentNullException(nameof(nodeMetadata));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            
            Name = vlNodeName;
            Category = category;
            Summary = _nodeMetadata.Description ?? _nodeMetadata.Title ?? $"Nodetool {_nodeMetadata.NodeType}";
            Remarks = BuildNodeRemarks();

            // Create input pin descriptions
            var inputPins = new List<IVLPinDescription>();
            
            // Add execution trigger input
            inputPins.Add(new PinDescription("Execute", typeof(bool), false, "⚡ Execute node"));

            // Add node-specific input pins from properties
            if (_nodeMetadata.Properties != null)
            {
                foreach (var property in _nodeMetadata.Properties)
                {
                    var (vlType, defaultValue) = MapNodeType(property.Type);
                    var summary = property.Description ?? property.Title ?? property.Name;
                    inputPins.Add(new PinDescription(property.Name, vlType ?? typeof(string), defaultValue, summary));
                }
            }

            Inputs = inputPins.AsReadOnly();

            // Create output pin descriptions
            var outputPins = new List<IVLPinDescription>();
            
            // Add node-specific output pins
            if (_nodeMetadata.Outputs != null)
            {
                foreach (var output in _nodeMetadata.Outputs)
                {
                    var (vlType, defaultValue) = MapNodeType(output.Type);
                    var summary = $"📤 {output.Name}";
                    outputPins.Add(new PinDescription(output.Name, vlType ?? typeof(string), defaultValue, summary));
                }
            }

            // Add standard status outputs
            outputPins.Add(new PinDescription("IsRunning", typeof(bool), false, "⏳ Execution status"));
            outputPins.Add(new PinDescription("Error", typeof(string), "", "❌ Error message"));

            Outputs = outputPins.AsReadOnly();
        }

        public string Name { get; }
        public string Category { get; }
        public bool Fragmented => false;
        public IReadOnlyList<IVLPinDescription> Inputs { get; }
        public IReadOnlyList<IVLPinDescription> Outputs { get; }
        public string Summary { get; }
        public string Remarks { get; }
        public IReadOnlyList<string> Tags => new List<string> { "Nodetool", _nodeMetadata.NodeType ?? "" }.AsReadOnly();
        public IVLNodeDescriptionFactory Factory => _factory;
        public IEnumerable<Message> Messages => Enumerable.Empty<Message>();
        
        // IVLNodeDescription requires an Invalidated event
        private readonly Subject<object> _invalidated = new Subject<object>();
        public IObservable<object> Invalidated => _invalidated;

        /// <summary>
        /// Creates an instance of the node
        /// </summary>
        public IVLNode CreateInstance(NodeContext nodeContext)
        {
            return new NodeBase(nodeContext, _nodeMetadata);
        }

        /// <summary>
        /// Updates the node description (not implemented for now)
        /// </summary>
        public IVLNodeDescription? Update(object? updateContext)
        {
            return this;
        }

        /// <summary>
        /// Map Nodetool type to VL type
        /// </summary>
        private (Type?, object?) MapNodeType(NodeTypeDefinition? nodeType)
        {
            if (nodeType == null || string.IsNullOrEmpty(nodeType.Type))
                return (typeof(string), "");

            return nodeType.Type.ToLowerInvariant() switch
            {
                "str" or "string" => (typeof(string), ""),
                "int" or "integer" => (typeof(int), 0),
                "float" or "number" => (typeof(float), 0.0f),
                "bool" or "boolean" => (typeof(bool), false),
                "list" or "array" => (typeof(string[]), new string[0]),
                "dict" or "object" => (typeof(object), null),
                "image" => (typeof(string), ""), // Image path or base64
                "audio" => (typeof(string), ""), // Audio path or data
                "video" => (typeof(string), ""), // Video path or data
                _ => (typeof(string), "")
            };
        }

        /// <summary>
        /// Build comprehensive node documentation
        /// </summary>
        private string BuildNodeRemarks()
        {
            var remarks = new List<string>();

            if (!string.IsNullOrEmpty(_nodeMetadata.Description))
                remarks.Add(_nodeMetadata.Description);

            remarks.Add($"Node Type: {_nodeMetadata.NodeType}");

            if (_nodeMetadata.Properties?.Count > 0)
                remarks.Add($"Inputs: {_nodeMetadata.Properties.Count}");

            if (_nodeMetadata.Outputs?.Count > 0)
                remarks.Add($"Outputs: {_nodeMetadata.Outputs.Count}");

            remarks.Add("⚡ Execution-based Nodetool node");

            return string.Join("\n", remarks);
        }

        /// <summary>
        /// Helper class for pin descriptions
        /// </summary>
        private class PinDescription : IVLPinDescription
        {
            public PinDescription(string name, Type type, object? defaultValue = null, string summary = "")
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue;
                Summary = summary;
                Remarks = string.Empty;
                Tags = new List<string>().AsReadOnly();
            }

            public string Name { get; }
            public Type Type { get; }
            public object? DefaultValue { get; }
            public string Summary { get; }
            public string Remarks { get; }
            public IReadOnlyList<string> Tags { get; }
        }
    }
} 