---
name: lite-sdd
description: Lightweight spec-driven development workflow that structures work into per-spec spec and plan artifacts under lite-sdd/<spec-name>/ (max 20 chars) and incremental stage-by-stage implementation with explicit user approval gates.
---

# Lite Spec-Driven Development (`lite-sdd`)

Use this workflow whenever the user invokes `/lite-sdd` or asks for structured, spec-driven implementation. Never skip phases or write application or CI/config code before `spec` and `plan` have been explicitly approved.

## Spec Directory Convention

- Store every specification and implementation plan under `lite-sdd/<spec-name>/` at the repository root.
- `<spec-name>` MUST be **at most 20 characters** long (use lowercase kebab-case, e.g. `base`, `github-ci`). If a user-supplied name exceeds 20 characters, shorten it to `<= 20` characters and explicitly note the chosen folder name.
- Inside `lite-sdd/<spec-name>/`, create and maintain:
  - `lite-sdd/<spec-name>/spec` — the specification for that feature, baseline, or pipeline.
  - `lite-sdd/<spec-name>/plan` — the staged implementation plan for that specification.
- Multiple specs can coexist side by side (for example `lite-sdd/base/` for an existing baseline and `lite-sdd/github-ci/` for a CI pipeline) without overwriting unrelated specs.

## Workflow Phases

### Phase 1: Specification (`lite-sdd/<spec-name>/spec`)
1. Analyze the user's request, target platform, tech stack, repository structure, and constraints.
2. Determine `<spec-name>` (`<= 20` chars) and create **ONLY** `lite-sdd/<spec-name>/spec`. Do not create `plan` or any implementation/workflow files yet.
3. Include in `lite-sdd/<spec-name>/spec`:
   - **Overview & Goals**: Concise summary of what is being built and why.
   - **Target Environment & Stack**: OS, architectures, languages, frameworks, runners, and key dependencies.
   - **Repository Layout**: Exact folder and file structure for the spec/change.
   - **Functional Requirements**: Concrete, testable requirements grouped logically.
   - **Constraints & Non-Goals**: Scope boundaries, simplicity rules, and documentation language requirements.
4. Stop and ask the user to review and approve `lite-sdd/<spec-name>/spec`.

### Phase 2: Implementation Plan (`lite-sdd/<spec-name>/plan`)
1. Proceed only after the user approves `lite-sdd/<spec-name>/spec`.
2. Create **ONLY** `lite-sdd/<spec-name>/plan` based on the approved `spec`. Do not write application or workflow files yet.
3. Break the work into small, self-contained, verifiable stages (matching any stage breakdown requested by the user).
4. For each stage in `lite-sdd/<spec-name>/plan`, specify:
   - **Files to create or modify**
   - **Implementation details & interfaces**
   - **Verification criteria** (how to build, run, or test the stage)
5. Stop and ask the user to review and approve `lite-sdd/<spec-name>/plan`.

### Phase 3: Staged Implementation
1. Proceed only after the user approves `lite-sdd/<spec-name>/plan`.
2. Implement **one stage at a time**, strictly in the order defined in `lite-sdd/<spec-name>/plan`.
3. Keep changes minimal, clean, idiomatic, and aligned with `lite-sdd/<spec-name>/spec`—avoid scope creep or unrequested abstractions.
4. After completing a stage:
   - Verify the stage using the defined build/test/lint checks where possible.
   - Summarize what was created/changed and how it was verified.
   - Wait for user confirmation before moving to the next stage (unless the user explicitly instructs to execute multiple stages at once).

## Rules
- Follow all language, formatting, and architectural constraints defined in the prompt and `lite-sdd/<spec-name>/spec`.
- If requirements are ambiguous during Phase 1, state clear, sensible defaults in `lite-sdd/<spec-name>/spec` and highlight them for user confirmation.
- Keep `spec` and `plan` concise, actionable, and free of boilerplate filler.