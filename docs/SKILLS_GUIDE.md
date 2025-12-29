# Skills Guide - Multi-Agent Development Workflow
**Project:** AI-Powered Privacy Browser
**Purpose:** Complete guide to using skills for orchestrated, parallel development

---

## 📚 Table of Contents
1. [Available Skills Overview](#available-skills-overview)
2. [Skills for Browser Development](#skills-for-browser-development)
3. [Skills NOT for Browser Development](#skills-not-for-browser-development)
4. [The Combined Workflow Strategy](#the-combined-workflow-strategy)
5. [Phase-by-Phase Skill Usage](#phase-by-phase-skill-usage)
6. [Parallel Agent Orchestration](#parallel-agent-orchestration)
7. [Common Workflows](#common-workflows)
8. [Skill Invocation Examples](#skill-invocation-examples)

---

## 🎯 Available Skills Overview

### **Installed Plugins:**
- ✅ **superpowers** (14 skills) - Core development workflow
- ✅ **feature-dev** (3 skills) - Architecture & quality analysis
- ✅ **episodic-memory** (1 skill) - Conversation search
- ⏸️ **frontend-design** (1 skill) - Modern UI design (Phase 9)
- ❌ **agent-sdk-dev** (3 skills) - NOT for browser (builds Agent SDK apps)

---

## ✅ Skills for Browser Development

### **Category 1: Workflow & Process (superpowers)**

#### **1.1 Planning Skills**

**`superpowers:brainstorming`**
- **When:** Before any creative work - creating features, building components
- **Purpose:** Explores user intent, requirements, design before implementation
- **Input:** Rough idea, requirements
- **Output:** Refined design, validated approach
- **Use in Browser:** Already used to design initial architecture
- **Example:** Used to turn two template docs into our final architecture

**`superpowers:writing-plans`**
- **When:** Design is complete, need detailed implementation tasks
- **Purpose:** Creates comprehensive plans with exact file paths, code examples, verification steps
- **Input:** High-level architecture or feature spec
- **Output:** Detailed task breakdown with:
  - Exact files to create/modify
  - Code examples
  - Test specifications
  - Verification steps
  - Dependency graph (what can be parallel)
- **Use in Browser:** Create detailed phase plans (Phase 1, Phase 2, etc.)
- **Example:** After architect designs "Network Interception", create 8 specific tasks

#### **1.2 Execution Skills**

**`superpowers:test-driven-development`**
- **When:** Before writing ANY feature or bugfix code
- **Purpose:** Enforces TDD workflow (RED → GREEN → REFACTOR)
- **Process:**
  1. Write test first
  2. Watch it fail (RED)
  3. Write minimal code to pass (GREEN)
  4. Refactor (REFACTOR)
- **Use in Browser:** EVERY feature implementation
- **Example:** Before implementing RuleEngine, write RuleEngineTests first

**`superpowers:subagent-driven-development`**
- **When:** Executing plans with independent tasks in current session
- **Purpose:** Dispatches fresh subagent for each task with code review between
- **Process:**
  1. Load implementation plan
  2. Identify parallelizable tasks
  3. Dispatch subagent per task
  4. Code review between tasks
  5. Integrate results
- **Use in Browser:** Execute Phase plans with parallel agents
- **Example:** Dispatch 4 agents for WebView2, Navigation, UI, Persistence

**`superpowers:executing-plans`**
- **When:** User provides a complete implementation plan
- **Purpose:** Loads plan, executes in batches, reports for review between batches
- **Process:**
  1. Load plan from file
  2. Execute batch 1
  3. Report for review
  4. Get approval
  5. Execute batch 2, etc.
- **Use in Browser:** Execute large multi-day plans with checkpoints
- **Example:** Execute entire Phase 1 plan with review after each batch

**`superpowers:dispatching-parallel-agents`**
- **When:** 3+ independent failures to investigate
- **Purpose:** Multiple agents investigate concurrent problems
- **Use in Browser:** When multiple bugs appear across different modules
- **Example:** If CSS injection, request blocking, and session persistence all fail

#### **1.3 Quality Assurance Skills**

**`superpowers:verification-before-completion`**
- **When:** Before claiming work is "complete", "fixed", or "passing"
- **Purpose:** Requires running verification commands and confirming output before success claims
- **Process:**
  1. Run verification (tests, build, manual check)
  2. Confirm output shows success
  3. THEN mark complete or claim success
- **Use in Browser:** Before marking any todo complete, before saying "it works"
- **Example:** Before claiming "Phase 1 done", run browser, test all features, show evidence

**`superpowers:requesting-code-review`**
- **When:** After completing major features or phases
- **Purpose:** Dispatches code-reviewer subagent to review implementation
- **Use in Browser:** After each phase completion (Phase 1, Phase 2, etc.)
- **Example:** After Phase 1 complete, request review before Phase 2

**`superpowers:receiving-code-review`**
- **When:** Receiving code review feedback from user
- **Purpose:** Requires technical rigor and verification, not blind implementation
- **Process:**
  1. Analyze feedback technically
  2. Verify claims
  3. Question if feedback seems wrong
  4. Implement with understanding
- **Use in Browser:** When user reviews code and suggests changes
- **Example:** User says "this will cause memory leak" - verify before blindly fixing

**`superpowers:systematic-debugging`**
- **When:** Encountering any bug, test failure, unexpected behavior
- **Purpose:** Four-phase debugging framework (not guess-and-check)
- **Process:**
  1. Root cause investigation (gather evidence)
  2. Pattern analysis (identify commonalities)
  3. Hypothesis testing (controlled experiments)
  4. Implementation (fix with verification)
- **Use in Browser:** When CSS injection fails, requests not blocking, etc.
- **Example:** WebView2 requests not intercepting - systematic investigation

#### **1.4 Workflow Management Skills**

**`superpowers:finishing-a-development-branch`**
- **When:** Implementation complete, tests pass, ready to integrate
- **Purpose:** Guides completion with structured options for merge/PR/cleanup
- **Use in Browser:** After each phase completion
- **Example:** Phase 1 done - merge to main or create PR?

**`superpowers:using-git-worktrees`**
- **When:** Starting feature work that needs isolation from current workspace
- **Purpose:** Creates isolated git worktrees with smart directory selection
- **Use in Browser:** When testing risky changes or parallel development
- **Example:** Test Phase 9 visual effects without breaking main branch

### **Category 2: Architecture & Quality (feature-dev)**

**`feature-dev:code-architect`**
- **When:** Before implementing any feature - design phase
- **Purpose:** Designs feature architectures by analyzing existing codebase patterns
- **Process:**
  1. Analyze existing code patterns
  2. Understand project conventions
  3. Design feature architecture
  4. Provide implementation blueprint with:
     - Specific files to create/modify
     - Component designs
     - Data flows
     - Build sequences
- **Use in Browser:** Design EVERY new feature before implementation
- **Example:** Design "Network Interception" architecture before coding

**`feature-dev:code-explorer`**
- **When:** Need to understand existing codebase features deeply
- **Purpose:** Traces execution paths, maps architecture layers, documents dependencies
- **Process:**
  1. Trace how features work
  2. Map architecture layers
  3. Understand patterns and abstractions
  4. Document dependencies
- **Use in Browser:** Understand existing features before adding new ones
- **Example:** Before adding tabs, explore how WebView2Host currently works

**`feature-dev:code-reviewer`**
- **When:** After code implementation, before claiming done
- **Purpose:** Reviews for bugs, logic errors, security, quality, conventions
- **Process:**
  1. Deep code analysis
  2. Check for:
     - Bugs and logic errors
     - Security vulnerabilities (SQL injection, XSS, etc.)
     - Code quality issues
     - Performance problems
     - Project convention adherence
  3. Confidence-based filtering (only high-priority issues)
- **Use in Browser:** After EVERY feature implementation, after EVERY phase
- **Example:** After implementing RuleEngine, deep review for security issues

### **Category 3: Memory & Context (episodic-memory)**

**`episodic-memory:remembering-conversations`**
- **When:** User asks "how should I..." or references past work
- **Purpose:** Searches conversation history for decisions/solutions
- **Use in Browser:** When user asks "how did we decide to handle X?"
- **Example:** "How did we decide on SQLite vs PostgreSQL for client?"

### **Category 4: Design Inspiration (frontend-design)**

**`frontend-design:frontend-design`**
- **When:** Phase 9 - Visual polish and modern UI design
- **Purpose:** Creates production-grade frontend interfaces (web-based)
- **Use in Browser:** Phase 9 - Get inspiration for WPF animations/effects
- **Example:** Generate modern glassmorphism concepts, adapt to WPF
- **Note:** Designed for web (React), we adapt concepts to WPF

---

## ❌ Skills NOT for Browser Development

### **agent-sdk-dev Plugin**

**CRITICAL: These skills are for BUILDING Agent SDK applications, NOT for using agents to build software.**

**`agent-sdk-dev:new-sdk-app`**
- **Purpose:** Create and setup a new Claude Agent SDK application
- **NOT for:** Our browser project
- **Example of WRONG use:** "Create browser using new-sdk-app" ❌
- **Example of RIGHT use:** "Create chatbot app using Agent SDK" ✅

**`agent-sdk-dev:agent-sdk-verifier-py`**
- **Purpose:** Verify Python Agent SDK app follows SDK best practices
- **NOT for:** Our browser (we're building C# WPF, not Agent SDK app)

**`agent-sdk-dev:agent-sdk-verifier-ts`**
- **Purpose:** Verify TypeScript Agent SDK app
- **NOT for:** Our browser

---

## 🎯 The Combined Workflow Strategy

### **Why Combine superpowers + feature-dev?**

**superpowers alone:**
- ✅ Great workflow and process
- ✅ TDD enforcement
- ✅ Parallel execution
- ❌ No deep architecture design
- ❌ Basic code review

**feature-dev alone:**
- ✅ Excellent architecture design
- ✅ Deep code review
- ❌ No TDD enforcement
- ❌ No parallel execution
- ❌ No structured workflow

**Combined (superpowers + feature-dev):**
- ✅ Excellent architecture (feature-dev)
- ✅ Structured workflow (superpowers)
- ✅ TDD enforcement (superpowers)
- ✅ Parallel execution (superpowers)
- ✅ Deep code review (feature-dev)
- ✅ Complete quality assurance

---

## 📅 Phase-by-Phase Skill Usage

### **Phase 1: Core Browser (Weeks 1-4)**

**Goal:** Working browser with basic navigation

**Skills Used:**
```
1. feature-dev:code-architect
   → Input: "Design core browser with WebView2, navigation, address bar"
   → Output: Architecture blueprint

2. superpowers:writing-plans
   → Input: Architecture blueprint
   → Output: 10 detailed tasks (4 can be parallel)

3. superpowers:subagent-driven-development
   → Dispatch 4 agents:
     - Agent A: WebView2Host service
     - Agent B: NavigationService
     - Agent C: WPF UI MainWindow
     - Agent D: UserDataFolder config
   → Each uses: test-driven-development
   → Each uses: verification-before-completion

4. feature-dev:code-reviewer
   → Review all code for security, bugs, quality

5. superpowers:verification-before-completion
   → Manual test: Browse 10 sites, verify features

6. superpowers:requesting-code-review
   → Final review before Phase 2

7. superpowers:finishing-a-development-branch
   → Merge Phase 1 work
```

### **Phase 2: Network Monitoring (Weeks 5-6)**

**Goal:** See and log network activity

**Skills Used:**
```
1. feature-dev:code-explorer
   → Analyze Phase 1 navigation to understand integration points

2. feature-dev:code-architect
   → Design NetworkInterceptor architecture

3. superpowers:writing-plans
   → Break into tasks

4. superpowers:subagent-driven-development
   → Parallel execution with TDD

5. feature-dev:code-reviewer
   → Security focus: check for data leaks, sensitive info logging

6. superpowers:verification-before-completion
   → Test on tracker-heavy sites

7. superpowers:requesting-code-review
   → Review before Phase 3
```

### **Phase 3: Rule System (Weeks 7-9)**

**Skills Used:**
```
1. feature-dev:code-explorer
   → Understand NetworkInterceptor from Phase 2

2. feature-dev:code-architect
   → Design RuleEngine architecture

3. superpowers:writing-plans
   → Detailed tasks

4. superpowers:subagent-driven-development
   → Parallel: RuleEngine, CSSInjector, JSInjector, Templates

5. feature-dev:code-reviewer
   → Security: Injection vulnerabilities, unsafe rules

6. superpowers:systematic-debugging (if needed)
   → If CSS/JS injection doesn't work

7. superpowers:verification-before-completion
   → Test templates on real sites

8. superpowers:requesting-code-review
   → Review before Phase 4
```

### **Phase 4-8: Server, Channels, AI, Profiles, Polish**

**Same pattern:**
1. Explore existing code (code-explorer)
2. Design architecture (code-architect)
3. Plan tasks (writing-plans)
4. Execute in parallel (subagent-driven-development with TDD)
5. Deep review (code-reviewer)
6. Verify (verification-before-completion)
7. Review (requesting-code-review)
8. Merge (finishing-a-development-branch)

### **Phase 9: Visual Enhancements (Optional)**

**Skills Used:**
```
1. frontend-design:frontend-design
   → Generate modern UI concepts (glassmorphism, animations)
   → Adapt web patterns to WPF

2. superpowers:writing-plans
   → Plan visual enhancements

3. Manual implementation with WPF UI library

4. superpowers:verification-before-completion
   → Verify smooth 60 FPS, no performance issues
```

---

## 🤖 Parallel Agent Orchestration

### **How Parallel Agents Work**

**Concept:** Multiple agents work on independent tasks simultaneously

**Tool:** `superpowers:subagent-driven-development`

**Requirements for Parallelization:**
1. **Tasks must be independent** (no shared files/state)
2. **No sequential dependencies** (Task B doesn't need Task A's output)
3. **Clear interfaces** (agents know what to build)

### **Example: Phase 1 Parallel Execution**

**Sequential Foundation (Must go first):**
```
Task 0: Project setup
├─ Create solution structure
├─ Install NuGet packages
├─ Configure appsettings.json
└─ Setup MVVM foundation
```

**Parallel Batch (Can work simultaneously):**
```
Agent A: WebView2Host Service
├─ File: BrowserApp.Core/Services/WebView2HostService.cs
├─ File: BrowserApp.Core/Interfaces/IWebView2HostService.cs
├─ Tests: WebView2HostServiceTests.cs
└─ Verify: Can initialize WebView2

Agent B: Navigation Service
├─ File: BrowserApp.Core/Services/NavigationService.cs
├─ File: BrowserApp.Core/Interfaces/INavigationService.cs
├─ Tests: NavigationServiceTests.cs
└─ Verify: URL vs search detection works

Agent C: WPF UI MainWindow
├─ File: BrowserApp.UI/Views/MainWindow.xaml
├─ File: BrowserApp.UI/ViewModels/MainViewModel.cs
├─ WPF UI styling applied
└─ Verify: Window renders with controls

Agent D: UserDataFolder Configuration
├─ File: BrowserApp.Core/Services/UserDataService.cs
├─ Tests: UserDataServiceTests.cs
└─ Verify: Cookies persist after restart
```

**Sequential Integration (After all agents finish):**
```
Task 5: Integration
├─ Wire up all services
├─ Dependency injection
├─ End-to-end testing
└─ Verify: Complete browser works
```

### **Agent Communication Protocol**

**Each agent receives:**
```json
{
  "task": "Implement NavigationService",
  "files_to_create": [
    "BrowserApp.Core/Services/NavigationService.cs",
    "BrowserApp.Core/Interfaces/INavigationService.cs"
  ],
  "tests_to_write": [
    "BrowserApp.Tests/Services/NavigationServiceTests.cs"
  ],
  "requirements": [
    "Detect if input is URL or search query",
    "Support multiple search engines",
    "Validate URLs before navigation"
  ],
  "dependencies": [],
  "verification": [
    "All tests pass",
    "Can detect 'github.com' as URL",
    "Can detect 'hello world' as search"
  ]
}
```

**Each agent returns:**
```json
{
  "status": "completed",
  "files_created": [...],
  "tests_written": true,
  "tests_passing": true,
  "verification_evidence": "Screenshot of test output",
  "issues_encountered": [],
  "integration_notes": "INavigationService implemented, ready for DI"
}
```

### **When NOT to Parallelize**

**Sequential Dependencies:**
```
❌ Cannot parallelize:
Task A: Create database schema
Task B: Create repository (needs schema from A)
Task C: Create service (needs repository from B)

✅ Must do: A → B → C
```

**Shared File Conflicts:**
```
❌ Cannot parallelize:
Agent 1: Modify MainWindow.xaml
Agent 2: Also modify MainWindow.xaml
Result: Merge conflict!

✅ Can parallelize:
Agent 1: Create WebView2Host.cs
Agent 2: Create NavigationService.cs
Result: Different files, no conflict
```

**Tight Integration:**
```
❌ Cannot parallelize:
Agent 1: Build UI component
Agent 2: Build ViewModel for same component
Result: Need constant communication

✅ Can parallelize:
Agent 1: Build NetworkMonitor component + ViewModel
Agent 2: Build RuleBuilder component + ViewModel
Result: Independent features
```

---

## 🔄 Common Workflows

### **Workflow 1: Implementing a New Feature**

```
┌─────────────────────────────────────────┐
│ New Feature Request: "Add Dark Mode"   │
└─────────────────────────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 1. CODE-EXPLORER     │
    │ Analyze UI theming   │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 2. CODE-ARCHITECT    │
    │ Design dark mode     │
    │ architecture         │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 3. WRITING-PLANS     │
    │ Create task list     │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 4. SUBAGENT-DRIVEN   │
    │ Execute with TDD     │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 5. CODE-REVIEWER     │
    │ Deep quality check   │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 6. VERIFICATION      │
    │ Prove it works       │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 7. REQUESTING-REVIEW │
    │ User approval        │
    └──────────────────────┘
            │
            ▼
         DONE ✅
```

### **Workflow 2: Fixing a Bug**

```
┌─────────────────────────────────────────┐
│ Bug: "CSS injection not working"       │
└─────────────────────────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 1. SYSTEMATIC-DEBUG  │
    │ Root cause analysis  │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 2. TDD               │
    │ Write failing test   │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 3. Fix + Verify      │
    │ Implement fix        │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 4. CODE-REVIEWER     │
    │ Check for regressions│
    └──────────────────────┘
            │
            ▼
         DONE ✅
```

### **Workflow 3: Multiple Bugs**

```
┌─────────────────────────────────────────┐
│ 3+ Bugs Appear Simultaneously           │
└─────────────────────────────────────────┘
            │
            ▼
    ┌──────────────────────────────────┐
    │ DISPATCHING-PARALLEL-AGENTS      │
    │                                  │
    │ Agent A: Investigate Bug 1       │
    │ Agent B: Investigate Bug 2       │
    │ Agent C: Investigate Bug 3       │
    └──────────────────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ Each agent uses:     │
    │ SYSTEMATIC-DEBUGGING │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ Consolidate findings │
    │ Fix sequentially     │
    └──────────────────────┘
            │
            ▼
         DONE ✅
```

### **Workflow 4: Complete Phase**

```
┌─────────────────────────────────────────┐
│ Start Phase 2: Network Monitoring       │
└─────────────────────────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 1. CODE-EXPLORER     │
    │ Understand Phase 1   │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 2. CODE-ARCHITECT    │
    │ Design architecture  │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 3. WRITING-PLANS     │
    │ Detailed tasks       │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 4. SUBAGENT-DRIVEN   │
    │ Parallel execution   │
    │ with TDD             │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 5. CODE-REVIEWER     │
    │ Deep analysis        │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 6. VERIFICATION      │
    │ Phase deliverable    │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 7. REQUESTING-REVIEW │
    │ Phase approval       │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 8. FINISHING-BRANCH  │
    │ Merge decision       │
    └──────────────────────┘
            │
            ▼
    Phase 2 Complete ✅
```

---

## 💻 Skill Invocation Examples

### **Example 1: Starting Phase 1**

**User says:** "Let's start Phase 1"

**Claude response:**
```
I'll use the combined workflow approach:

1. Using feature-dev:code-architect to design Phase 1 architecture...
```

**Skill invocation:**
```
Skill: feature-dev:code-architect
Args: Design core browser architecture with WebView2, navigation,
      address bar, and WPF UI integration
```

**After architect completes:**
```
2. Using superpowers:writing-plans to create detailed task breakdown...
```

**Skill invocation:**
```
Skill: superpowers:writing-plans
Prompt: Create detailed implementation plan for Phase 1 based on
        architecture. Include file paths, test specs, verification steps.
```

**After plan created:**
```
3. Plan created with 10 tasks, 4 can be parallelized.
   Ready to use superpowers:subagent-driven-development?
```

**User approves:**
```
4. Dispatching 4 agents...
```

**Skill invocation:**
```
Skill: superpowers:subagent-driven-development
Prompt: Execute Phase 1 plan with parallel agents for tasks 2-5
```

### **Example 2: Debugging CSS Injection**

**User says:** "CSS injection isn't working"

**Claude response:**
```
I'll use superpowers:systematic-debugging to investigate properly
instead of guess-and-check.
```

**Skill invocation:**
```
Skill: superpowers:systematic-debugging
Prompt: CSS injection not working on pages. Need to investigate:
        - Is ExecuteScriptAsync being called?
        - What's the timing (DOMContentLoaded vs NavigationCompleted)?
        - Are there any errors in WebView2 console?
        - Does manual script execution work?
```

### **Example 3: Code Review After Feature**

**User says:** "I finished implementing the RuleEngine"

**Claude response:**
```
Great! Before moving on, I'll use feature-dev:code-reviewer to do a
deep analysis of the implementation.
```

**Skill invocation:**
```
Skill: feature-dev:code-reviewer
Prompt: Review RuleEngine implementation for:
        - Security issues (code injection, unsafe eval)
        - Logic errors (rule matching, priority)
        - Performance (O(n²) loops, memory leaks)
        - Project conventions (MVVM, naming)
        Focus on high-confidence issues only.
```

### **Example 4: Multiple Bugs**

**User says:** "3 things broke: request blocking, CSS injection, and sessions"

**Claude response:**
```
Since there are 3 independent issues, I'll use
superpowers:dispatching-parallel-agents to investigate concurrently.
```

**Skill invocation:**
```
Skill: superpowers:dispatching-parallel-agents
Prompt: Investigate 3 failures:
        Agent A: Request blocking not working
        Agent B: CSS injection failing
        Agent C: Sessions not persisting
        Each agent use systematic-debugging approach.
```

---

## 📋 Checklist for Every Session

**At session start, Claude should:**
1. ✅ Read `docs/SKILLS_GUIDE.md` (this file)
2. ✅ Read `docs/DEVELOPMENT_PROTOCOL.md`
3. ✅ Read `docs/plans/2025-11-16-browser-architecture.md`
4. ✅ Check git status
5. ✅ Ask: "Where did we leave off?"
6. ✅ Use `episodic-memory:remembering-conversations` if needed

**For any new feature:**
1. ✅ Use `feature-dev:code-explorer` (understand existing)
2. ✅ Use `feature-dev:code-architect` (design new)
3. ✅ Use `superpowers:writing-plans` (plan tasks)
4. ✅ Use `superpowers:subagent-driven-development` (execute)
5. ✅ Use `feature-dev:code-reviewer` (deep review)
6. ✅ Use `superpowers:verification-before-completion` (verify)
7. ✅ Use `superpowers:requesting-code-review` (user approval)

**For any bug:**
1. ✅ Use `superpowers:systematic-debugging` (not guess-and-check)
2. ✅ Use `superpowers:test-driven-development` (write test first)
3. ✅ Fix + verify
4. ✅ Use `feature-dev:code-reviewer` (check for regressions)

**For any completed phase:**
1. ✅ Use `superpowers:verification-before-completion`
2. ✅ Use `superpowers:requesting-code-review`
3. ✅ Use `superpowers:finishing-a-development-branch`

---

## 🚨 Common Mistakes to Avoid

### **Mistake 1: Not Using Skills**
❌ "I'll just implement NavigationService directly"
✅ "I'll use code-architect → writing-plans → subagent-driven-development"

### **Mistake 2: Wrong Skill**
❌ "Using agent-sdk-dev:new-sdk-app to create browser"
✅ "agent-sdk-dev is for Agent SDK apps, not our browser"

### **Mistake 3: Skipping TDD**
❌ "I'll write tests after implementing"
✅ "Use test-driven-development skill for every feature"

### **Mistake 4: Claiming Done Without Verification**
❌ "Implementation complete, moving to next task"
✅ "Use verification-before-completion to prove it works first"

### **Mistake 5: Parallelizing Dependent Tasks**
❌ "Dispatch agents for schema + repository + service (all depend on each other)"
✅ "Schema → Repository → Service (sequential, they depend on each other)"

### **Mistake 6: No Code Review**
❌ "Feature done, merging to main"
✅ "Use code-reviewer → fix issues → requesting-code-review → then merge"

### **Mistake 7: Guess-and-Check Debugging**
❌ "Let me try adding a delay... maybe that fixes it"
✅ "Use systematic-debugging to investigate root cause"

---

## 🎯 Success Metrics

**You'll know the skills are working when:**
- ✅ Every feature has architecture designed BEFORE coding
- ✅ Tests are written BEFORE implementation (RED → GREEN → REFACTOR)
- ✅ Tasks execute in parallel when possible (faster development)
- ✅ Code reviews catch real issues (security, bugs, quality)
- ✅ Nothing claimed "done" without verification evidence
- ✅ Debugging is systematic, not random trial-and-error
- ✅ Every phase gets thorough review before moving on

**Timeline evidence:**
- Without skills: Phase 1 takes 5-6 days (sequential, no review)
- With skills: Phase 1 takes 2-3 days (parallel, architected, reviewed)

**Quality evidence:**
- Without skills: Bugs found in Phase 3 from Phase 1 code
- With skills: Bugs caught in Phase 1 review, fixed before Phase 2

---

## 📚 Related Documents

- `docs/DEVELOPMENT_PROTOCOL.md` - Development practices and anti-patterns
- `docs/plans/2025-11-16-browser-architecture.md` - Complete architecture
- `docs/QUICK_REFERENCE.md` - WebView2 & WPF UI code snippets

---

## 🔄 Document Updates

**Last Updated:** December 2025
**Next Review:** After Phase 1 completion (add learnings)
**Update Trigger:** New skills installed, workflow changes, lessons learned

---

**Remember:** Skills are tools to ensure quality, speed, and consistency. Use them proactively, not reactively. The workflow is designed to catch issues early, when they're cheap to fix, rather than late when they're expensive.

**When in doubt:** Check this guide, follow the workflow, invoke the appropriate skill.
