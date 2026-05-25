# Contributing

## Branch model

| Branch | Role |
|--------|------|
| **`enhanced`** | Default integration branch (`origin/HEAD`). **Direct pushes are not allowed** — land work via pull request here. ([Ruleset payload](.github/rulesets/enhanced-pr-required.json)) |
| **`stable`** | Release line synced from **`enhanced`** via PR. Same protection: no ordinary direct pushes. ([Ruleset payload](.github/rulesets/stable-pr-required.json)) |
| **`feat/*`** | Short-lived branches; open PRs targeting **`enhanced`** (promotion PRs **`enhanced` → `stable`** look like targeting `stable`). |

### Promoting work to `stable`

1. Merge changes into **`enhanced`** via **pull request** (cannot push directly once rulesets are active).
2. When ready for consumers pinning **`#stable`**, open **Pull request: `enhanced` → `stable`** and merge when ready.

## GitHub rules on `enhanced` and `stable`

Both branches use **repository rulesets** with the same policy:

| Rule | Meaning |
|------|--------|
| **Pull request** | Commits reach the branch only through a **merged PR** (`required_approving_review_count: 0` — no mandated reviewers, but merges still happen via GitHub merge UI). Merge / squash / rebase are allowed. |
| **Deletion** | Branch deletion is limited to bypass actors (if configured). |
| **Non-fast-forward** | Force-push is blocked. |

- **`enhanced`:** ruleset **`enhanced-require-pull-request`** — `.github/rulesets/enhanced-pr-required.json`
- **`stable`:** ruleset **`stable-require-pull-request`** — `.github/rulesets/stable-pr-required.json`

Configure or inspect: **GitHub → Repository → Settings → Rules → Rulesets**.

> **Note:** The JSON files are documentation snapshots. They were applied with the [Create a repository ruleset](https://docs.github.com/en/rest/repos/rules#create-a-repository-ruleset) API.
