# NodeTool SDK for vvvv

This folder contains VL documents and help patches for executing NodeTool nodes and workflows from vvvv gamma.

## vvvv setup

nuget install VL.Nodetool
open help/Nodetool_Help.vl

## Connection defaults

NodeTool starts the backend service on localhost and uses port `7777`.

- Worker WebSocket: `ws://127.0.0.1:7777/ws`
- HTTP API: `http://127.0.0.1:7777`
