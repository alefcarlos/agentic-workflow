# Meu workflow para agentes de código

## Agent Skills Discovery Server

```bash
docker run --rm --name skills-server -p 8080:8080 $(docker build -q .)
```

```http
GET http://localhost:8080/.well-known/skills/index.json
```

## Agentes de código

```bash
mkdir -p .agents/skills
mkdir -p .agents/rules
```

- [opencode](https://opencode.ai/)

### Plugins

- [opencode-pty](https://github.com/shekohex/opencode-pty.git)

## MCP Servers

- [aspire](https://aspire.dev/dashboard/mcp-server/)
- playwright
- [backlog](https://github.com/MrLesk/Backlog.md)

## dotnet

```bash
curl -o .agents/rules/aspire-guidelines.md https://raw.githubusercontent.com/alefcarlos/agentic-workflow/refs/heads/main/rules/dotnet/aspire-guidelines.md
```

### task-management

```bash
mkdir -p .agents/skills/implement-task
mkdir -p .agents/skills/plan-feature
mkdir -p .agents/skills/refinar-task

curl -o .agents/rules/backlogmd-guidelines.md https://raw.githubusercontent.com/alefcarlos/agentic-workflow/refs/heads/main/rules/task-management/backlogmd-guidelines.md

curl -o .agents/skills/implement-task/SKILL.md https://raw.githubusercontent.com/alefcarlos/agentic-workflow/refs/heads/main/skills/implement-task/SKILL.md
curl -o .agents/skills/plan-feature/SKILL.md https://raw.githubusercontent.com/alefcarlos/agentic-workflow/refs/heads/main/skills/plan-feature/SKILL.md
curl -o .agents/skills/refinar-task/SKILL.md https://raw.githubusercontent.com/alefcarlos/agentic-workflow/refs/heads/main/skills/refinar-task/SKILL.md
```
