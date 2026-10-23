---
engine:
  id: booroute-gpt
  display-name: BooRoute GPT demo
  description: One fixed OpenAI request; no model tools or executable output.
  mcp: false
  runtime-id: booroute-gpt
  auth:
    - role: github
      secret: GITHUB_TOKEN
    - role: optional-model
      secret: OPENAI_API_KEY
  provider:
    name: openai
  models:
    default: gpt-4.1-mini
  behaviors:
    supported-env-var-keys: [GITHUB_TOKEN, OPENAI_API_KEY]
    execution:
      command-name: node
      args: [.github/scripts/demo-review.cjs, generate]
      step-name: Generate bounded GPT demo feedback
      write-timestamp: true
---

One-shot GPT engine for the inline review demo. The trusted script handles the
HTTP deadline, validates JSON, and never executes model output.

gh-aw validates that at least one auth binding exists. The automatic GitHub token
satisfies that check, so the model key remains optional and can use the fallback.
