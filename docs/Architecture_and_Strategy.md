# Strategy & Architecture Document: Azure Pipelines to GitHub Actions Migration

## 1. Architectural Overview & Blueprint

### 1.1 Scope & Purpose
This document provides a complete architectural blueprint, governance strategy, security posture, and migration guide for transitioning .NET enterprise application CI/CD pipelines from Azure DevOps Pipelines to GitHub Actions.

### 1.2 Component Topology
The migration design incorporates modern platform engineering principles, enforcing DRY (Don't Repeat Yourself) design via composite actions and enterprise-grade reusable workflows.

```
       [ Pull Request (Target: main/develop) ]
                        │
                        ▼
  ┌──────────────────────────────────────────────────┐
  │ Workflows: pr-validation.yml                    │
  │ (Caller Workflow with Concurrency & Least Priv)  │
  └─────────────────────┬────────────────────────────┘
                        │
                        ▼
  ┌──────────────────────────────────────────────────┐
  │ Reusable Workflow:                               │
  │ .github/workflows/reusable-dotnet-validation.yml│
  └─────────────────────┬────────────────────────────┘
                        │
       ┌────────────────┴────────────────┐
       ▼                                 ▼
┌──────────────────────────────┐ ┌──────────────────────────────┐
│ Composite Action:             │ │ Test Execution & Coverage:   │
│ .github/actions/             │ │ - xUnit Execution           │
│ dotnet-setup-build           │ │ - Cobertura Coverage         │
│ - SDK Setup                  │ │ - TRX Log Generation         │
│ - NuGet Caching              │ │ - Artifact Uploads           │
│ - dotnet build               │ │ - Step Summary Publishing    │
└──────────────────────────────┘ └──────────────────────────────┘
```

---

## 2. Governance, Permissions & Security Framework

### 2.1 Least-Privilege Permissions Matrix
By default, GitHub repository tokens (`GITHUB_TOKEN`) must be restricted at the organization and repository levels to `read-all`. Workflows explicitly request top-level or job-level scopes:

| Permission Scope | Tier Granted | Rationale |
| :--- | :--- | :--- |
| `contents` | `read` | Allows checkout of repository source code without write access. |
| `pull-requests` | `write` | Allows commenting test run metrics and coverage summaries directly onto PRs. |
| `checks` | `write` | Permits reporting test status and execution annotations into GitHub Check Runs. |
| `packages` | `none` | Omitted for PR validation to prevent unauthorized package pushes. |

### 2.2 OpenID Connect (OIDC) & Secret Zero
- **Elimination of Static Secrets:** Migrate away from long-lived Azure Service Principals stored as client secrets.
- **Federated Credentials:** Leverage GitHub OIDC identity provider (`https://token.actions.githubusercontent.com`) paired with Azure User-Assigned Managed Identities.
- **Subject Binding:** Scope Azure role assignments strictly to repository branches (`repo:org/repo:environment:Production` or `repo:org/repo:pull_request`).

---

## 3. Migration Mapping & Technical Translation Matrix

| Feature / Domain | Legacy Azure Pipelines | GitHub Actions Target Pattern |
| :--- | :--- | :--- |
| **Trigger Mechanism** | `pr: branches: include: [ main ]` | `on: pull_request: branches: [ main ]` |
| **Execution Environment** | `pool: vmImage: 'ubuntu-latest'` | `runs-on: ubuntu-latest` |
| **SDK Provisioning** | `UseDotNet@2` | `actions/setup-dotnet@v4` |
| **Caching Layer** | Implicit / Task-based cache | `actions/cache@v4` (Key: `${{ runner.os }}-nuget-...`) |
| **Test Collection** | `DotNetCoreCLI@2 (command: test)` | `dotnet test --collect:"XPlat Code Coverage"` |
| **Artifact Publishing** | `PublishCodeCoverageResults@1` | `actions/upload-artifact@v4` + Step Summaries |
| **Modular Logic** | Task Groups / Step Templates | Composite Actions (`action.yml`) |
| **Standardized Flow** | YAML Templates (`- template:`) | Reusable Workflows (`workflow_call`) |

---

## 4. Rollback & Migration Contingency Strategy

### 4.1 Phase-Based Migration Rollout
1. **Phase 1 (Shadow Phase):** Run GitHub Actions PR validation in parallel with existing Azure Pipelines. Do not enforce GHA as a mandatory status check.
2. **Phase 2 (Dual Validation):** Enable GHA as a required status check while leaving Azure Pipelines active in non-blocking mode.
3. **Phase 3 (Primary Cutover):** Decommission Azure Pipelines PR triggers (`pr: none`). Make GHA the sole blocking check for pull request merges.

### 4.2 Instant Rollback Plan
If critical workflow vulnerabilities or blocking pipeline failures occur:
1. Re-enable PR validation triggers in `azure-pipelines.yml`.
2. Disable GitHub Actions workflow status requirement in repository **Branch Protection Rules** / **Repository Rulesets**.
3. Set the GitHub Actions workflow `on.pull_request` trigger to draft or commented state via immediate hotfix branch merge.

---

## 5. Architectural Trade-offs Analysis

| Architectural Decision | Trade-offs & Analysis |
| :--- | :--- |
| **Composite Action vs. Reusable Workflow** | **Composite Actions** bundle multiple steps into a single reusable step, but share job contexts and lack job-level execution isolation. **Reusable Workflows** run complete jobs with dedicated runners, outputs, and isolation, but introduce minor runner spin-up overhead. |
| **Inline Caching vs. Setup-Dotnet Built-in Caching** | Built-in setup-dotnet cache is simpler, but `actions/cache@v4` allows custom key hashing (`hashFiles('**/*.csproj')`) and flexible restore-keys across OS runners. |
| **Artifact Storage vs. In-Line Reporting** | Uploading full coverage XML/TRX as artifacts guarantees auditability, while rendering Markdown step summaries gives developer-centric instant visibility. |

---

## 6. Verification & Local Execution

To validate the configuration locally:
1. Ensure `.NET 8 SDK` is installed locally (`dotnet --version` outputs `8.0.x`).
2. Run `dotnet restore`.
3. Run `dotnet build --configuration Release --no-restore`.
4. Run `dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage"`.
