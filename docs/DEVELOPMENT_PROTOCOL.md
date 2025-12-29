# Development Protocol & Claude Guidelines
**Project:** AI-Powered Privacy Browser
**Purpose:** Ensure consistent, high-quality development across all sessions

---

## 🎯 Core Principles

1. **Function First, Polish Later** - Phases 1-8 before Phase 9 (visual enhancements)
2. **Test-Driven Development** - Write tests before implementation
3. **Verify Before Claiming** - Prove it works before saying it's done
4. **Code Review After Major Features** - Get review before moving to next phase
5. **Systematic Debugging** - Investigate properly, don't guess-and-check

---

## 🛠️ Available Skills & When to Use Them

### **MANDATORY Skills (Use Proactively)**

#### 1. `superpowers:test-driven-development`
**When:** Before writing ANY feature or bugfix implementation
**Process:**
1. Write the test first
2. Watch it fail (RED)
3. Write minimal code to pass (GREEN)
4. Refactor (REFACTOR)

**Example:**
```
Task: Implement RuleEngine.Evaluate()
❌ DON'T: Start writing RuleEngine code
✅ DO: Write test for RuleEngine first, then implement
```

#### 2. `superpowers:verification-before-completion`
**When:** Before claiming work is "complete", "fixed", or "passing"
**Process:**
1. Run verification commands (tests, build, manual check)
2. Confirm output shows success
3. THEN mark todo as complete or claim success

**Example:**
```
Task: Network interceptor blocks trackers
❌ DON'T: "I've implemented blocking, it should work"
✅ DO: Run browser, navigate to test site, verify tracker is blocked, show evidence
```

#### 3. `superpowers:systematic-debugging`
**When:** Encountering ANY bug, test failure, or unexpected behavior
**Process:**
1. Root cause investigation (gather evidence)
2. Pattern analysis (identify commonalities)
3. Hypothesis testing (controlled experiments)
4. Implementation (fix with verification)

**Example:**
```
Issue: CSS injection not working
❌ DON'T: Try random fixes (change timing, add delays, etc.)
✅ DO: Use systematic-debugging to investigate why
```

#### 4. `superpowers:requesting-code-review`
**When:** After completing major features or phases
**Process:**
1. Complete implementation
2. All tests pass
3. Invoke skill to dispatch code-reviewer agent
4. Address feedback before moving on

**Example:**
```
After Phase 1 (Core Browser) complete:
✅ Invoke superpowers:requesting-code-review
```

### **Workflow Skills (Use When Appropriate)**

#### 5. `superpowers:writing-plans`
**When:** Design is complete, need detailed implementation tasks
**Output:** Comprehensive plan with file paths, code examples, verification steps
**Use For:** Creating detailed phase plans (Phase 1, Phase 2, etc.)

#### 6. `superpowers:executing-plans`
**When:** User provides a complete implementation plan
**Process:** Load plan → execute in batches → review between batches

#### 7. `superpowers:subagent-driven-development`
**When:** Executing plans with independent tasks
**Process:** Dispatch subagent per task → code review between tasks

#### 8. `superpowers:finishing-a-development-branch`
**When:** Implementation complete, tests pass, ready to integrate
**Output:** Structured options for merge/PR/cleanup

#### 9. `superpowers:receiving-code-review`
**When:** Receiving code review feedback from user
**Process:** Verify feedback technically, don't blindly implement

#### 10. `superpowers:dispatching-parallel-agents`
**When:** 3+ independent failures to investigate
**Process:** Multiple agents investigate concurrently

### **Utility Skills**

#### 11. `episodic-memory:remembering-conversations`
**When:** User asks "how should I..." or references past work
**Process:** Search conversation history for decisions/solutions

#### 12. `frontend-design:frontend-design`
**When:** Building web components/pages (may be useful for WPF design inspiration)

---

## 📋 Development Workflow by Phase

### **Starting a New Phase**

1. **Read this protocol** to remember available skills
2. **Check episodic memory** for past decisions
3. **Use `superpowers:writing-plans`** to create detailed phase plan
4. **Review plan with user** before starting implementation

### **During Implementation**

1. **For each feature:**
   - Use `superpowers:test-driven-development` (RED → GREEN → REFACTOR)
   - Write TodoWrite todos to track progress
   - Mark todos in_progress before starting
   - Mark todos completed ONLY after verification

2. **When encountering bugs:**
   - Use `superpowers:systematic-debugging`
   - Don't guess-and-check
   - Document findings

3. **Before claiming completion:**
   - Use `superpowers:verification-before-completion`
   - Run tests, build, manual verification
   - Show evidence (console output, screenshots, etc.)

### **Completing a Phase**

1. **Verify all phase deliverables met**
2. **Run full test suite** (if exists)
3. **Use `superpowers:requesting-code-review`**
4. **Address review feedback**
5. **Use `superpowers:finishing-a-development-branch`** for merge decision
6. **Update phase checklist in architecture doc**

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

## 🗂️ File Organization Checklist

### **When Creating New Files**

**C# Classes:**
- [ ] In correct project (BrowserApp.Core, BrowserApp.UI, etc.)
- [ ] In correct namespace
- [ ] Interface defined (if service)
- [ ] Summary XML comments on public methods

**XAML Views:**
- [ ] In BrowserApp.UI/Views/
- [ ] Corresponding ViewModel in BrowserApp.UI/ViewModels/
- [ ] DataContext set correctly
- [ ] No logic in code-behind (use ViewModel)

**Tests:**
- [ ] In corresponding test project
- [ ] Named correctly: `ClassNameTests.cs`
- [ ] Follows Arrange-Act-Assert pattern

---

## 🎨 Code Style Guidelines

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

**At the beginning of EVERY session, Claude should:**

1. **Read this protocol** (`docs/DEVELOPMENT_PROTOCOL.md`)
2. **Read architecture** (`docs/plans/2025-11-16-browser-architecture.md`)
3. **Check git status** to see current state
4. **Ask user:** "Where did we leave off? Continuing [Phase X] or starting new work?"
5. **Search episodic memory** if user references past decisions
6. **Create/update TodoWrite** for current work

---

## 🚦 Common Scenarios

### **Scenario: Starting Phase 1 (Core Browser)**
```
1. Read protocol + architecture docs
2. Use superpowers:writing-plans to create Phase 1 plan
3. Review plan with user
4. For each feature in plan:
   a. Use superpowers:test-driven-development
   b. Create TodoWrite todos
   c. Implement with verification
5. After phase: Use superpowers:requesting-code-review
6. Use superpowers:finishing-a-development-branch
```

### **Scenario: Bug appears during development**
```
1. DON'T guess-and-check
2. Use superpowers:systematic-debugging
3. Follow 4-phase process
4. Document root cause
5. Write test to prevent regression
6. Fix with verification
```

### **Scenario: User asks "how should we handle X?"**
```
1. Use episodic-memory:remembering-conversations
2. Search for past discussions on X
3. Reference architecture decisions
4. Propose approach consistent with existing design
```

### **Scenario: Multiple independent bugs found**
```
1. If 3+ independent issues:
   Use superpowers:dispatching-parallel-agents
2. If 1-2 issues:
   Use superpowers:systematic-debugging sequentially
```

---

## 🎯 Phase-Specific Reminders

### **Phase 1: Core Browser (Weeks 1-4)**
- Start with WPF project setup
- WebView2 integration is critical - test thoroughly
- MVVM from day 1 (don't refactor later)
- Search engine detection (URL vs query)
- Password manager config (WebView2 settings)

**Key Verification:**
- Can navigate to websites
- Can search from address bar
- Can go back/forward
- Passwords save and autofill

### **Phase 2: Network Monitoring (Weeks 5-6)**
- WebResourceRequested event hook
- SQLite logging (don't block UI thread)
- DataGrid performance (virtualization)
- Export to CSV (test with large datasets)

**Key Verification:**
- All requests logged
- No UI freezing
- Export works
- Filters work

### **Phase 3: Rule System (Weeks 7-9)**
- JSON parser (validate input)
- Rule evaluation (performance critical - < 10ms)
- CSS/JS injection timing (DOMContentLoaded)
- Pre-built templates (test on real sites)

**Key Verification:**
- Blocking works
- CSS injection works (no flicker)
- JS injection works
- Templates work on target sites

### **Phase 4-8: Server, Channels, AI, Profiles, Polish**
- Follow same TDD → verify → review workflow
- Each phase builds on previous (don't break existing)
- Integration tests between phases

---

## 🔄 Continuous Practices

**Every Coding Session:**
- [ ] Read this protocol
- [ ] TodoWrite for current work
- [ ] TDD for new features
- [ ] Verify before claiming done
- [ ] Git commit with meaningful messages

**Every Feature:**
- [ ] Test first
- [ ] Implement minimal code
- [ ] Verify it works
- [ ] Mark todo complete

**Every Phase:**
- [ ] Code review
- [ ] Update architecture doc (mark checkboxes)
- [ ] Git commit/branch decision
- [ ] Celebrate progress 🎉

---

## 🚨 Anti-Patterns to Avoid

❌ **"It should work now"** → ✅ "I verified it works by [evidence]"
❌ **Skipping tests "to save time"** → ✅ TDD from start (saves debugging time)
❌ **"Let me try this..."** → ✅ Systematic debugging with hypothesis
❌ **Marking todos complete prematurely** → ✅ Verify first, then mark
❌ **Business logic in XAML code-behind** → ✅ MVVM pattern always
❌ **Guess-and-check debugging** → ✅ Systematic investigation

---

## 📚 Key Documents to Reference

1. **This Protocol** - `docs/DEVELOPMENT_PROTOCOL.md` (read every session)
2. **Architecture** - `docs/plans/2025-11-16-browser-architecture.md` (design reference)
3. **Phase Plans** - `docs/plans/phase-X-plan.md` (created via writing-plans skill)
4. **Git Commits** - Check recent commits for context

---

## 💡 Remember

**This is a master's thesis project - quality matters more than speed.**

- Take time to do it right
- Use skills proactively
- Verify everything
- Get code reviews
- Build clean, maintainable code
- The architecture supports easy visual updates later (Phase 9)

**When in doubt:**
1. Check this protocol
2. Use appropriate skill
3. Ask user for clarification

---

**Last Updated:** 2025-11-16 (Initial creation)
**Next Review:** When starting Phase 1 implementation
