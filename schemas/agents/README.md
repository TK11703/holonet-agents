# Agent response schemas

These files are the source-controlled response contracts for the agents in the Holonet workflow.

- `orchestrator-response.schema.json` is used by `holonet-orchestrator`.
- `specialist-response.schema.json` is shared by the character, event, Jedi, planet, Sith, and vehicle agents.
- `agent-response-contracts.json` maps every exact Foundry agent name to its response format.
- `holonet-synthesizer-agent` returns plain text and therefore has no JSON schema.

The schemas match the strict runtime parsing in `AgentResponseParser`: property names are case-sensitive,
all declared fields are required, enum values are lowercase, and additional properties are rejected.

These files document the contracts and can be used when configuring structured output in Microsoft
Foundry. They are not automatically pushed to Foundry by this application.