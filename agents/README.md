# Agent source files

Store each agent's instruction text and local domain knowledge under a directory named with the exact
Microsoft Foundry agent name:

```text
agents/
  holonet-orchestrator/
    instructions.md
  holonet-character-agent/
    instructions.md
    knowledge/
      *.json
  holonet-event-agent/
    instructions.md
    knowledge/
      *.json
  holonet-jedi-agent/
    instructions.md
    knowledge/
      *.json
  holonet-planet-agent/
    instructions.md
    knowledge/
      *.json
  holonet-sith-agent/
    instructions.md
    knowledge/
      *.json
  holonet-vehicle-agent/
    instructions.md
    knowledge/
      *.json
  holonet-synthesizer-agent/
    instructions.md
```

Use Markdown for instruction files so prompts remain readable and reviewable in pull requests. Keep
each specialist's JSON knowledge beside its instructions to make ownership explicit. The orchestrator
and synthesizer do not currently require domain knowledge files.

Response shapes remain centralized in [`../schemas/agents`](../schemas/agents/README.md), since all
specialists share the same schema.

These files are source artifacts only. The application does not automatically upload instructions or
knowledge to Microsoft Foundry. Do not commit credentials, secrets, private data, or generated indexes.