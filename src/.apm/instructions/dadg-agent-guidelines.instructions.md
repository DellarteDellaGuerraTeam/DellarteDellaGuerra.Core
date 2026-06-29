---
description: Behavioral coding guidelines for agent work in the DADG repository.
---

# DADG Agent Guidelines

These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## Think Before Coding

Do not assume. Do not hide confusion. Surface tradeoffs.

- State assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop, name what is confusing, and ask.

## Simplicity First

Use the minimum code that solves the problem.

- No features beyond what was asked.
- No abstractions for single-use code.
- No speculative flexibility or configurability.
- No error handling for impossible scenarios.
- If a solution is much larger than it needs to be, simplify it.

## Surgical Changes

Touch only what the task requires.

- Do not improve adjacent code, comments, or formatting opportunistically.
- Do not refactor unrelated code.
- Match existing style, even where another style would be preferable.
- Mention unrelated dead code instead of deleting it.
- Remove only imports, variables, functions, or files that your own changes made unused.

Every changed line should trace directly to the user's request.

## Goal-Driven Execution

Turn tasks into verifiable goals.

- Add validation: write tests for invalid inputs, then make them pass.
- Fix a bug: reproduce it with a test or live check, then make it pass.
- Refactor code: verify tests before and after where practical.

For multi-step tasks, state a brief plan with verification points.

## Project-Specific Context

- For general codebase context, use the DADG project overview instruction and the `dadg-project-context` skill.
- For live Bannerlord debugging, GABS, Rider debugger issues, CLR exceptions, or fix/relaunch/verify loops, use the `bannerlord-debug` skill.
- For conditional breakpoint syntax, JetBrains dialog handling, and decompiled source breakpoints, use `bannerlord-debug/references/jetbrains-debugger-tips.md`.
