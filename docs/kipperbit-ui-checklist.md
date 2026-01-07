# KipperbitUI Local Run Checklist (no tests)

1) Ensure you are on the `kipperbit` branch.
2) From repo root, run `aspire run` (or `dotnet run --project AIChat.AppHost`).
3) Open the Chat UI from the Aspire dashboard (the `chatui` endpoint).
4) In the sidebar Runner panel, set:
   - Workspace root (optional; leave blank to use defaults).
   - Repos root (optional; leave blank to use defaults).
   - Mode (`read-only` or `workspace-write`).
5) Send a prompt and confirm the streaming response starts with:
   - `[kipperbit-ui stub | mode=...]`
6) Optionally hit Cancel to confirm the stream stops.

Notes:
- Defaults are read from `KIPPERBIT_SHARED_ROOT`, `KIPPERBIT_REPOS_ROOT`, and `KIPPERBIT_MODE`.
- For now the runner is a stub; swap in the real CLI bridge when ready.
