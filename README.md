# holonet-agents

A .NET 10 Blazor Web App (interactive server rendering) that provides chat windows for AI agents
that already exist in a Microsoft Foundry project. No database is used; direct-agent conversation
state lives in Foundry responses and UI state lives in the browser session.

## Holonet workflow

Each message submitted in **Holonet Chat** runs this workflow:

1. `holonet-orchestrator` classifies the request as a character, vehicle, planet, event, or other request.
2. A classified request is sent to the corresponding `holonet-*-agent` specialist. The `other` category skips this step.
3. The specialist response, or the original request for `other`, is sent to `holonet-synthesizer-agent`.
4. The synthesizer's plain-text response is displayed in the chat.

The Foundry project must contain the orchestrator, synthesizer, character, vehicle, planet, and event
agents using those exact names. Each workflow stage starts an independent Foundry response because
response-chain IDs belong to the agent that created them.

## Projects

- `src/HolonetAgents.Web` — the Blazor Web App.

## Configuration

Set the Foundry settings in `src/HolonetAgents.Web/appsettings.json`, user secrets, or environment
variables:

| Setting | Description |
| --- | --- |
| `Foundry:Endpoint` | Required. Project endpoint, e.g. `https://<resource>.services.ai.azure.com/api/projects/<project>`. |
| `Foundry:DefaultAgentId` | Optional. Agent pre-selected in the chat window. |
| `Foundry:ManagedIdentityClientId` | Optional. Client id of a user-assigned managed identity. |
| `Foundry:RunTimeoutSeconds` | Optional. How long to wait for an agent run (default `120`). |

Example using user secrets (keeps values out of source control):

```bash
cd src/HolonetAgents.Web
dotnet user-secrets init
dotnet user-secrets set "Foundry:Endpoint" "https://<resource>.services.ai.azure.com/api/projects/<project>"
```

Authentication uses `DefaultAzureCredential`, so sign in locally with `az login` and make sure the
identity has the **Azure AI User** role on the Foundry project. In Azure, assign the same role to the
app's managed identity.

## Run

```bash
dotnet run --project src/HolonetAgents.Web
```

Then browse to **Holonet Chat** to use the workflow, or open a specialist page to chat directly with
that agent. Use **New conversation** to clear the current browser chat and direct-agent response chain.
