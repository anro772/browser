# Skills Guide - Multi-Agent Development Workflow
**Project:** AI-Powered Privacy Browser
**Purpose:** Complete guide to using skills, agents, and MCP tools for orchestrated development
**Last Updated:** January 2026

---

## 📚 Table of Contents
1. [Available Tools Overview](#available-tools-overview)
2. [Skills (User-Invocable)](#skills-user-invocable)
3. [Task Agents (Subagents)](#task-agents-subagents)
4. [MCP Servers & Tools](#mcp-servers--tools)
5. [The Combined Workflow Strategy](#the-combined-workflow-strategy)
6. [Phase-by-Phase Usage](#phase-by-phase-usage)
7. [Parallel Agent Orchestration](#parallel-agent-orchestration)
8. [Common Workflows](#common-workflows)
9. [Invocation Examples](#invocation-examples)

---

## 🎯 Available Tools Overview

### **Three Categories of Tools:**

| Category | What It Is | How to Use | Purpose |
|----------|------------|------------|---------|
| **Skills** | User-invocable commands (`/skill-name`) | Skill tool | Specific workflows (commit, review, design) |
| **Task Agents** | Specialized subagents | Task tool with `subagent_type` | Architecture, exploration, review |
| **MCP Servers** | External tools/APIs | `mcp__servername__toolname` | Library docs, browser testing |

---

## 🔧 Skills (User-Invocable)

Skills are invoked with `/skill-name` or via the Skill tool. They provide specialized workflows.

### **1. feature-dev:feature-dev**
- **Invoke:** `/feature-dev` or Skill tool
- **Purpose:** Guided feature development with codebase understanding and architecture focus
- **When to Use:** Starting any new feature that needs architectural planning
- **Process:**
  1. Analyzes existing codebase patterns
  2. Designs feature architecture
  3. Provides implementation guidance
- **Use in Browser:** Major features (Network Interception, Rule Engine, etc.)

### **2. commit-commands:commit**
- **Invoke:** `/commit`
- **Purpose:** Create a git commit with proper formatting
- **When to Use:** After completing a task/feature
- **Features:**
  - Analyzes staged changes
  - Generates commit message
  - Co-authored-by attribution

### **3. commit-commands:commit-push-pr**
- **Invoke:** `/commit-push-pr`
- **Purpose:** Commit, push, and open a PR in one workflow
- **When to Use:** Feature complete, ready for review
- **Process:**
  1. Creates commit
  2. Pushes to remote
  3. Opens pull request with summary

### **4. commit-commands:clean_gone**
- **Invoke:** `/clean_gone`
- **Purpose:** Clean up git branches marked as [gone] (deleted on remote)
- **When to Use:** Periodic repository cleanup
- **Features:**
  - Finds branches deleted on remote
  - Removes associated worktrees
  - Cleans local tracking

### **5. code-review:code-review**
- **Invoke:** `/code-review`
- **Purpose:** Code review a pull request
- **When to Use:** Reviewing incoming PRs
- **Checks:**
  - Bugs and logic errors
  - Security vulnerabilities
  - Code quality
  - Project conventions

### **6. frontend-design:frontend-design**
- **Invoke:** `/frontend-design`
- **Purpose:** Create distinctive, production-grade frontend interfaces
- **When to Use:** Phase 9 - Visual polish and modern UI
- **Features:**
  - Modern design patterns (glassmorphism, etc.)
  - Production-ready code
  - Avoids generic AI aesthetics
- **Note:** Designed for web (React/HTML), adapt concepts to WPF

### **7. agent-sdk-dev:new-sdk-app**
- **Invoke:** `/new-sdk-app`
- **Purpose:** Create and setup a new Claude Agent SDK application
- **⚠️ NOT for Browser Project:** This creates Agent SDK apps, not software built BY agents
- **When to Use:** Building chatbots, AI assistants using Agent SDK

---

## 🤖 Task Agents (Subagents)

Task agents are specialized subprocesses launched via the Task tool. They handle complex, multi-step work autonomously.

### **How to Launch:**
```
Task tool with:
- subagent_type: "agent-name"
- prompt: "detailed task description"
- model: "sonnet" | "opus" | "haiku" (optional)
- run_in_background: true/false (optional)
```

### **1. Explore Agent**
- **Type:** `Explore`
- **Purpose:** Fast codebase exploration and searching
- **Capabilities:**
  - Find files by patterns (`**/*.cs`)
  - Search code for keywords
  - Answer questions about codebase structure
- **Thoroughness Levels:** "quick", "medium", "very thorough"
- **Use in Browser:** Understanding existing code before modifications

### **2. Plan Agent**
- **Type:** `Plan`
- **Purpose:** Software architect for designing implementation plans
- **Capabilities:**
  - Step-by-step implementation plans
  - Identifies critical files
  - Considers architectural trade-offs
- **Use in Browser:** Planning each phase before execution

### **3. feature-dev:code-architect**
- **Type:** `feature-dev:code-architect`
- **Purpose:** Designs feature architectures by analyzing existing patterns
- **Output:**
  - Specific files to create/modify
  - Component designs
  - Data flows
  - Build sequences
- **Use in Browser:** Design EVERY feature before implementation

### **4. feature-dev:code-explorer**
- **Type:** `feature-dev:code-explorer`
- **Purpose:** Deep analysis of existing codebase features
- **Capabilities:**
  - Traces execution paths
  - Maps architecture layers
  - Documents dependencies
  - Understands patterns and abstractions
- **Use in Browser:** Before adding to existing features

### **5. feature-dev:code-reviewer**
- **Type:** `feature-dev:code-reviewer`
- **Purpose:** Reviews code for bugs, security, quality
- **Checks:**
  - Bugs and logic errors
  - Security vulnerabilities (OWASP Top 10)
  - Code quality issues
  - Project convention adherence
- **Features:** Confidence-based filtering (only high-priority issues)
- **Use in Browser:** After EVERY implementation, before claiming done

### **6. General-Purpose Agent**
- **Type:** `general-purpose`
- **Purpose:** Complex multi-step tasks, research, searching
- **Use When:** Task doesn't fit specialized agents

### **7. Bash Agent**
- **Type:** `Bash`
- **Purpose:** Command execution (git, npm, docker, etc.)
- **Use in Browser:** Build commands, package management

---

## 🌐 MCP Servers & Tools

MCP (Model Context Protocol) servers provide external capabilities.

### **1. context7 - Library Documentation**
- **Purpose:** Fetch up-to-date documentation for any library
- **Tools:**
  - `mcp__context7__resolve-library-id` - Find library ID
  - `mcp__context7__query-docs` - Query documentation
- **Use in Browser:**
  - WebView2 documentation
  - WPF patterns
  - NuGet package docs

**Example Usage:**
```
1. resolve-library-id: "WebView2", "how to intercept network requests"
2. query-docs: "/microsoft/webview2", "intercept and modify HTTP requests"
```

### **2. playwright - Browser Automation**
- **Purpose:** Automate browser testing and interactions
- **Key Tools:**
  - `mcp__playwright__browser_navigate` - Go to URL
  - `mcp__playwright__browser_snapshot` - Accessibility snapshot (better than screenshot)
  - `mcp__playwright__browser_click` - Click elements
  - `mcp__playwright__browser_type` - Type text
  - `mcp__playwright__browser_take_screenshot` - Capture screenshot
  - `mcp__playwright__browser_evaluate` - Run JavaScript
  - `mcp__playwright__browser_console_messages` - Get console logs
  - `mcp__playwright__browser_network_requests` - Get network activity
- **Use in Browser:**
  - Test browser behavior
  - Verify UI interactions
  - Debug rendering issues
  - Automated E2E testing

**Example Usage:**
```
1. browser_navigate: "https://example.com"
2. browser_snapshot: Get page structure
3. browser_click: Click element by ref
4. browser_take_screenshot: Capture result
```

---

## 🎯 The Combined Workflow Strategy

### **Why Combine Skills + Agents + MCP?**

| Tool Type | Strength | Weakness |
|-----------|----------|----------|
| Skills alone | Guided workflows | Limited scope |
| Agents alone | Deep analysis | No structured process |
| MCP alone | External data | No implementation |

**Combined approach gives:**
- ✅ Guided workflows (Skills)
- ✅ Deep architecture design (Agents)
- ✅ Thorough code review (Agents)
- ✅ Up-to-date documentation (context7)
- ✅ Automated testing (playwright)

---

## 📅 Phase-by-Phase Usage

### **Phase 1: Core Browser**

**Goal:** Working browser with basic navigation

**Tools Used:**
```
1. Task (feature-dev:code-architect)
   → Design core browser architecture

2. Task (Plan)
   → Create detailed implementation plan

3. context7
   → Query WebView2 documentation

4. Task (feature-dev:code-reviewer)
   → Review implementation

5. playwright
   → Test browser functionality
```

### **Phase 2: Network Monitoring**

**Tools Used:**
```
1. Task (feature-dev:code-explorer)
   → Analyze Phase 1 navigation code

2. Task (feature-dev:code-architect)
   → Design NetworkInterceptor

3. context7
   → WebView2 CoreWebView2.WebResourceRequested docs

4. Task (feature-dev:code-reviewer)
   → Security review (data leak check)

5. playwright
   → Test on tracker-heavy sites
```

### **Phase 3-8: Subsequent Phases**

**Same pattern:**
1. Explore existing code (code-explorer)
2. Design architecture (code-architect)
3. Query relevant docs (context7)
4. Implement with TDD
5. Deep review (code-reviewer)
6. Test (playwright)
7. Commit (/commit)

### **Phase 9: Visual Enhancements**

**Tools Used:**
```
1. frontend-design skill
   → Generate modern UI concepts

2. playwright
   → Verify 60 FPS, smooth animations
```

---

## 🤖 Parallel Agent Orchestration

### **How to Run Agents in Parallel**

Launch multiple Task tool calls in a single message:

```
[Message with multiple Task calls]
├── Task 1: code-architect for Module A
├── Task 2: code-architect for Module B
└── Task 3: code-architect for Module C
```

### **Requirements for Parallelization:**
1. **Tasks must be independent** (no shared files)
2. **No sequential dependencies** (B doesn't need A's output)
3. **Clear interfaces** (agents know what to build)

### **Example: Phase 1 Parallel Execution**

**Sequential Foundation (First):**
```
Task 0: Project setup
├── Create solution structure
├── Install NuGet packages
└── Configure settings
```

**Parallel Batch (Simultaneously):**
```
Agent A: WebView2Host Service
├── File: Services/WebView2HostService.cs
└── Tests: WebView2HostServiceTests.cs

Agent B: Navigation Service
├── File: Services/NavigationService.cs
└── Tests: NavigationServiceTests.cs

Agent C: WPF UI MainWindow
├── File: Views/MainWindow.xaml
└── File: ViewModels/MainViewModel.cs

Agent D: UserDataFolder Config
├── File: Services/UserDataService.cs
└── Tests: UserDataServiceTests.cs
```

**Sequential Integration (After):**
```
Task 5: Integration
├── Wire up all services
├── Dependency injection
└── End-to-end testing
```

### **When NOT to Parallelize**

**Sequential Dependencies:**
```
❌ Cannot parallelize:
Task A: Create database schema
Task B: Create repository (needs A)
Task C: Create service (needs B)

✅ Must do: A → B → C
```

**Shared File Conflicts:**
```
❌ Cannot parallelize:
Agent 1: Modify MainWindow.xaml
Agent 2: Also modify MainWindow.xaml

✅ Can parallelize:
Agent 1: Create WebView2Host.cs
Agent 2: Create NavigationService.cs
```

---

## 🔄 Common Workflows

### **Workflow 1: Implementing a New Feature**

```
┌─────────────────────────────────────────┐
│ New Feature Request: "Add Dark Mode"    │
└─────────────────────────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 1. CODE-EXPLORER     │
    │ Analyze UI theming   │
    │ (Task agent)         │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 2. CODE-ARCHITECT    │
    │ Design architecture  │
    │ (Task agent)         │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 3. CONTEXT7          │
    │ Query WPF theming    │
    │ documentation        │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 4. IMPLEMENT         │
    │ Write code with TDD  │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 5. CODE-REVIEWER     │
    │ Deep quality check   │
    │ (Task agent)         │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 6. PLAYWRIGHT        │
    │ Test functionality   │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 7. /COMMIT           │
    │ Commit changes       │
    └──────────────────────┘
            │
            ▼
         DONE ✅
```

### **Workflow 2: Fixing a Bug**

```
┌─────────────────────────────────────────┐
│ Bug: "CSS injection not working"        │
└─────────────────────────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 1. EXPLORE           │
    │ Find relevant code   │
    │ (Task agent)         │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 2. CONTEXT7          │
    │ Check WebView2 docs  │
    │ for script execution │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 3. Write failing test│
    │ (TDD approach)       │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 4. Fix + Verify      │
    │ Implement fix        │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 5. CODE-REVIEWER     │
    │ Check for regressions│
    └──────────────────────┘
            │
            ▼
         DONE ✅
```

### **Workflow 3: Research Unknown API**

```
┌─────────────────────────────────────────┐
│ Need: WebView2 request interception     │
└─────────────────────────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 1. CONTEXT7          │
    │ resolve-library-id   │
    │ "WebView2"           │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 2. CONTEXT7          │
    │ query-docs           │
    │ "intercept requests" │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 3. Apply knowledge   │
    │ to implementation    │
    └──────────────────────┘
            │
            ▼
         DONE ✅
```

### **Workflow 4: Test UI Visually**

```
┌─────────────────────────────────────────┐
│ Test: Verify browser UI works           │
└─────────────────────────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 1. PLAYWRIGHT        │
    │ browser_navigate     │
    │ to test page         │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 2. PLAYWRIGHT        │
    │ browser_snapshot     │
    │ (accessibility tree) │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 3. PLAYWRIGHT        │
    │ browser_click        │
    │ browser_type         │
    │ (interact)           │
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 4. PLAYWRIGHT        │
    │ browser_screenshot   │
    │ (visual verification)│
    └──────────────────────┘
            │
            ▼
         DONE ✅
```

---

## 💻 Invocation Examples

### **Example 1: Design Feature Architecture**

```
Task tool:
- subagent_type: "feature-dev:code-architect"
- prompt: "Design network request interception architecture for
  WebView2-based browser. Need to intercept, log, and optionally
  block HTTP requests. Consider performance and thread safety."
```

### **Example 2: Explore Existing Code**

```
Task tool:
- subagent_type: "Explore"
- prompt: "Find all files related to navigation handling.
  Look for URL parsing, navigation events, and address bar logic.
  Thoroughness: medium"
```

### **Example 3: Query Library Documentation**

```
1. mcp__context7__resolve-library-id:
   - libraryName: "WebView2"
   - query: "How to intercept network requests in WebView2"

2. mcp__context7__query-docs:
   - libraryId: "/microsoft/webview2" (from step 1)
   - query: "CoreWebView2.WebResourceRequested event handler"
```

### **Example 4: Browser Testing**

```
1. mcp__playwright__browser_navigate:
   - url: "http://localhost:5000"

2. mcp__playwright__browser_snapshot
   → Get accessibility tree

3. mcp__playwright__browser_click:
   - element: "Navigation button"
   - ref: "button[0]"

4. mcp__playwright__browser_take_screenshot:
   - filename: "test-result.png"
```

### **Example 5: Code Review**

```
Task tool:
- subagent_type: "feature-dev:code-reviewer"
- prompt: "Review the RuleEngine implementation in
  src/Services/RuleEngine.cs. Focus on:
  - Security issues (code injection, unsafe eval)
  - Logic errors (rule matching, priority)
  - Performance (memory leaks, O(n²) loops)
  Only report high-confidence issues."
```

### **Example 6: Create Commit**

```
Skill tool:
- skill: "commit"
→ Analyzes changes, generates message, creates commit
```

---

## 📋 Checklist for Every Session

**At session start:**
1. ✅ Read `docs/SKILLS_GUIDE.md` (this file)
2. ✅ Read `docs/DEVELOPMENT_PROTOCOL.md`
3. ✅ Check git status
4. ✅ Ask: "Where did we leave off?"

**For any new feature:**
1. ✅ Use `Explore` agent (understand existing)
2. ✅ Use `feature-dev:code-architect` (design new)
3. ✅ Use `context7` (query relevant docs)
4. ✅ Implement with TDD
5. ✅ Use `feature-dev:code-reviewer` (deep review)
6. ✅ Use `playwright` (test functionality)
7. ✅ Use `/commit` (commit changes)

**For any bug:**
1. ✅ Use `Explore` agent (find relevant code)
2. ✅ Use `context7` (check documentation)
3. ✅ Write failing test (TDD)
4. ✅ Fix + verify
5. ✅ Use `feature-dev:code-reviewer` (check regressions)

**For completed phase:**
1. ✅ Use `feature-dev:code-reviewer` (full review)
2. ✅ Use `playwright` (E2E testing)
3. ✅ Use `/commit-push-pr` (PR workflow)

---

## 🚨 Common Mistakes to Avoid

### **Mistake 1: Wrong Tool for Task**
❌ "Using agent-sdk-dev:new-sdk-app to create browser"
✅ "agent-sdk-dev is for Agent SDK apps, not our browser"

### **Mistake 2: Not Querying Docs**
❌ "I'll guess how WebView2 interception works"
✅ "Use context7 to get accurate WebView2 documentation"

### **Mistake 3: Skipping Code Review**
❌ "Feature done, committing now"
✅ "Use code-reviewer agent → fix issues → then commit"

### **Mistake 4: Not Testing**
❌ "UI looks good in code"
✅ "Use playwright to verify actual behavior"

### **Mistake 5: Parallelizing Dependent Tasks**
❌ "Dispatch agents for schema + repository + service (all depend on each other)"
✅ "Schema → Repository → Service (sequential)"

### **Mistake 6: Forgetting TDD**
❌ "I'll write tests after implementing"
✅ "Write failing test → implement → verify pass"

---

## 📚 Related Documents

- [DEVELOPMENT_PROTOCOL.md](DEVELOPMENT_PROTOCOL.md) - Development practices and anti-patterns
- [plans/2025-11-16-browser-architecture.md](plans/2025-11-16-browser-architecture.md) - Complete architecture
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - WebView2 & WPF UI code snippets

---

## 🔄 Document Updates

**Last Updated:** January 2026
**Previous Version:** December 2025 (had outdated "superpowers" and "episodic-memory" plugins)
**Changes:**
- Removed references to unavailable plugins (superpowers, episodic-memory)
- Removed shadcn-ui MCP (not needed for WPF project)
- Added MCP servers: context7, playwright
- Updated skills list to current availability
- Added Task agents section
- Updated workflows to use current tools

---

**Remember:** Use the right tool for the job:
- **Skills** for structured workflows (commit, review, design)
- **Task Agents** for complex analysis (architecture, exploration, review)
- **MCP Tools** for external capabilities (docs, browser testing)

**When in doubt:** This guide has the current list of available tools.
