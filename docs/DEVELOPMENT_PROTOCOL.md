# Development Protocol
**Project:** AI-Powered Privacy Browser
**Purpose:** Ensure consistent, high-quality development across all sessions
**Last Updated:** January 2026

---

## 🎯 Core Principles

1. **Function First, Polish Later** - Phases 1-8 before Phase 9 (visual enhancements)
2. **Test-Driven Development** - Write tests before implementation
3. **Verify Before Claiming** - Prove it works before saying it's done
4. **Code Review After Major Features** - Get review before moving to next phase
5. **Systematic Debugging** - Investigate properly, don't guess-and-check

---

## 🛠️ Available Tools

### **Skills (User-Invocable via `/command`)**

| Skill | Command | When to Use |
|-------|---------|-------------|
| Feature Development | `/feature-dev` | Starting new features |
| Create Commit | `/commit` | After completing work |
| Commit + PR | `/commit-push-pr` | Ready for review |
| Code Review | `/code-review` | Reviewing PRs |
| Frontend Design | `/frontend-design` | Phase 9 UI work |

### **Task Agents (via Task tool)**

| Agent | Type | When to Use |
|-------|------|-------------|
| Explore | `Explore` | Finding code, understanding structure |
| Plan | `Plan` | Designing implementation approach |
| Code Architect | `feature-dev:code-architect` | Feature architecture design |
| Code Explorer | `feature-dev:code-explorer` | Deep analysis of existing code |
| Code Reviewer | `feature-dev:code-reviewer` | Quality/security review |

### **MCP Tools**

| Server | Purpose |
|--------|---------|
| **context7** | Library documentation (WebView2, WPF, etc.) |
| **playwright** | Browser automation and testing |

---

## 📋 Development Workflow

### **Starting a New Phase**

1. **Read documentation** (this file, architecture doc)
2. **Explore existing code** using `Explore` agent
3. **Design architecture** using `feature-dev:code-architect`
4. **Query documentation** using context7 for APIs
5. **Review plan with user** before implementation

### **During Implementation**

1. **For each feature:**
   - Write test first (TDD: RED → GREEN → REFACTOR)
   - Use TodoWrite to track progress
   - Mark todos in_progress before starting
   - Mark todos completed ONLY after verification

2. **When encountering bugs:**
   - Don't guess-and-check
   - Use `Explore` agent to find relevant code
   - Use context7 to check documentation
   - Document findings

3. **Before claiming completion:**
   - Run tests, build, manual verification
   - Show evidence (console output, playwright screenshots)

### **Completing a Phase**

1. Verify all phase deliverables met
2. Run full test suite
3. Use `feature-dev:code-reviewer` for deep review
4. Address review feedback
5. Use `/commit-push-pr` for merge
6. Update phase checklist in architecture doc

---

## ✅ Quality Gates

### **Feature-Level Gates**
- [ ] Tests written BEFORE implementation
- [ ] Tests pass
- [ ] Code follows MVVM pattern (UI ↔ ViewModel ↔ Service)
- [ ] No business logic in XAML code-behind
- [ ] Manual verification completed

### **Phase-Level Gates**
- [ ] All phase todos completed
- [ ] All tests pass
- [ ] Phase deliverable works as specified
- [ ] Code review completed
- [ ] No known critical bugs

### **Never Skip These**
- ❌ Never claim "it works" without running it
- ❌ Never mark todo complete without verification
- ❌ Never merge phase without code review
- ❌ Never implement without tests (unless prototyping)

---

## 🤖 Agent-Maintained Files

The following files are maintained by AI agents and should be updated as needed during development:

| File | Responsibility | When to Update |
|------|----------------|----------------|
| `.gitignore` | Agent | When new file patterns need exclusion (build artifacts, IDE files, secrets) |
| `docs/DEVELOPMENT_PROTOCOL.md` | Agent | When workflow changes or new tools are added |
| `docs/SKILLS_GUIDE.md` | Agent | When new skills/agents become available |

### **Git Workflow**
- **Commits are created by user** - Agents prepare changes, user commits manually
- **Agent updates .gitignore** - When new patterns are identified (e.g., new IDE, build output)
- **Agent does NOT push** - Unless explicitly requested by user

---

## 🗂️ File Organization

### **C# Classes**
- [ ] In correct project (BrowserApp.Core, BrowserApp.UI, etc.)
- [ ] In correct namespace
- [ ] Interface defined (if service)
- [ ] Summary XML comments on public methods

### **XAML Views**
- [ ] In BrowserApp.UI/Views/
- [ ] Corresponding ViewModel in BrowserApp.UI/ViewModels/
- [ ] DataContext set correctly
- [ ] No logic in code-behind (use ViewModel)

### **Tests**
- [ ] In corresponding test project
- [ ] Named correctly: `ClassNameTests.cs`
- [ ] Follows Arrange-Act-Assert pattern

---

## 🎨 Code Style

### **C# Conventions**
- PascalCase for classes, methods, properties
- camelCase for private fields, parameters
- Prefix interfaces with `I` (e.g., `IRuleEngine`)
- Use `async`/`await` for I/O operations
- Dependency injection via constructor

### **XAML Conventions**
- Use WPF UI components (Wpf.Ui namespace)
- Binding over code-behind
- Resources in separate ResourceDictionary files
- Consistent spacing (4-space indent)

### **Project Structure**
```
BrowserApp/
├── BrowserApp.UI/          # WPF (Views, ViewModels, Styles)
├── BrowserApp.Core/        # Business Logic (Services, Models)
├── BrowserApp.Data/        # Data Access (EF Core, Repositories)
├── BrowserApp.AI/          # AI Integration (LLM Client)
├── BrowserApp.Server/      # Server (API, Controllers)
└── BrowserApp.Tests/       # Unit/Integration Tests
```

---

## 📝 Session Start Checklist

**At the beginning of EVERY session:**

1. ✅ Read `docs/DEVELOPMENT_PROTOCOL.md` (this file)
2. ✅ Read `docs/plans/2025-11-16-browser-architecture.md`
3. ✅ Check git status
4. ✅ Ask: "Where did we leave off?"
5. ✅ Create/update TodoWrite for current work

---

## 🚦 Common Scenarios

### **Starting Phase 1 (Core Browser)**
```
1. Read protocol + architecture docs
2. Use feature-dev:code-architect to design
3. Use context7 for WebView2 documentation
4. Review plan with user
5. Implement with TDD (test first)
6. Verify with playwright
7. Use feature-dev:code-reviewer
8. Use /commit-push-pr
```

### **Bug During Development**
```
1. DON'T guess-and-check
2. Use Explore agent to find relevant code
3. Use context7 to check API documentation
4. Document root cause
5. Write test to prevent regression
6. Fix with verification
```

### **Research Unknown API**
```
1. Use context7: resolve-library-id
2. Use context7: query-docs
3. Apply knowledge to implementation
```

### **Test UI Behavior**
```
1. Use playwright: browser_navigate
2. Use playwright: browser_snapshot
3. Use playwright: browser_click/type
4. Use playwright: browser_screenshot
```

---

## 🎯 Phase Reminders

### **Phase 1: Core Browser**
- Start with WPF project setup
- WebView2 integration is critical - test thoroughly
- MVVM from day 1 (don't refactor later)
- Search engine detection (URL vs query)

**Verify:** Navigate to sites, search, back/forward, passwords

### **Phase 2: Network Monitoring**
- WebResourceRequested event hook
- SQLite logging (don't block UI thread)
- DataGrid virtualization for performance

**Verify:** All requests logged, no UI freeze, export works

### **Phase 3: Rule System**
- JSON parser (validate input)
- Rule evaluation (< 10ms performance)
- CSS/JS injection timing (DOMContentLoaded)

**Verify:** Blocking works, CSS/JS injection works, templates work

### **Phase 4-8: Server, Channels, AI, Profiles, Polish**
- Follow same TDD → verify → review workflow
- Each phase builds on previous
- Integration tests between phases

---

## 🚨 Anti-Patterns to Avoid

❌ **"It should work now"** → ✅ "I verified it works by [evidence]"
❌ **Skipping tests "to save time"** → ✅ TDD from start
❌ **"Let me try this..."** → ✅ Systematic debugging
❌ **Marking todos complete prematurely** → ✅ Verify first
❌ **Business logic in XAML code-behind** → ✅ MVVM always
❌ **Guess-and-check debugging** → ✅ Systematic investigation

---

## 📚 Key Documents

| Document | Purpose |
|----------|---------|
| [DEVELOPMENT_PROTOCOL.md](DEVELOPMENT_PROTOCOL.md) | This file - development practices |
| [SKILLS_GUIDE.md](SKILLS_GUIDE.md) | Complete tools reference |
| [plans/2025-11-16-browser-architecture.md](plans/2025-11-16-browser-architecture.md) | Architecture design |
| [QUICK_REFERENCE.md](QUICK_REFERENCE.md) | WebView2 & WPF code snippets |

---

## 💡 Remember

**This is a master's thesis project - quality matters more than speed.**

- Take time to do it right
- Use appropriate tools for each task
- Verify everything
- Get code reviews
- Build clean, maintainable code

**When in doubt:**
1. Check this protocol
2. Check SKILLS_GUIDE.md for available tools
3. Ask user for clarification

---

**Last Updated:** January 2026
**Changes:** Simplified from old superpowers plugin references to current official Claude Code tools
