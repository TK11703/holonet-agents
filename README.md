# holonet-agents

A .NET 10 Blazor Web App (interactive server rendering) that provides a chat window for talking to
the AI agents that already exist in an Azure AI Foundry project. No database is used — conversation
state lives in the Foundry thread and in the browser session.

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

Then browse to the **Chat** page, pick an agent, and send messages. Use **New conversation** to start
a fresh Foundry thread.
