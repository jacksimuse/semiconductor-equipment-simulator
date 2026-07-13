# Project Handoff

## Git Repository

- Remote URL: not configured yet
- Clone command after push:

```powershell
git clone <REMOTE_URL>
```

## Unity Version

```text
m_EditorVersion: 6000.5.3f1
m_EditorVersionWithRevision: 6000.5.3f1 (c2eb47b3a2a9)
```

Source file:

```text
ProjectSettings/ProjectVersion.txt
```

## Claude Desktop MCP Servers

Claude Desktop config was not found at:

```text
%APPDATA%\Claude\claude_desktop_config.json
```

When moving to another computer, copy the `mcpServers` object from that file and add it to Claude Desktop on the new machine.

Example shape:

```json
{
  "mcpServers": {}
}
```

## Project Claude Settings

Project-level Claude permissions are stored in:

```text
.claude/settings.local.json
```

Codex project MCP setting:

```toml
[mcp_servers.UnityMCP]
url = "http://127.0.0.1:8080/mcp"
```
