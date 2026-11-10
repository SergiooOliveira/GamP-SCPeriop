# Teacher Role Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Admin/Supervisor/Supervisionado with Support/Teacher/Supervisor/Student: Teachers own pathways, dates and supervisor assignment; Supervisors only grade their assigned students and can request deadline extensions; Support can do everything.

**Architecture:** Roles stay an int enum on `User` (values for Supervisor and Student unchanged; 2 becomes Support, new 3 is Teacher), so a single EF migration flips former Admins to Teacher. Every permission rule lives in `AccessService` (server) and is unit-tested; controllers only call it. Extension requests and the missing-supervisor reminders are small services with their own tests; the Blazor client follows the server rules.

**Tech Stack:** ASP.NET Core 10, EF Core 10 (SQL Server / LocalDB), Blazor WebAssembly, new xUnit test project with EF Core InMemory.

**Spec:** `docs/superpowers/specs/2026-11-10-teacher-role-design.md`

## Global Constraints

- On-screen role names, exactly: **Suporte**, **Professor**, **Supervisor**, **Estudante**. Code identifiers in English, UI text in Portuguese (pt-PT).
- `UserRole` values: `Supervisor = 0`, `Student = 1`, `Support = 2`, `Teacher = 3`. JWT role claim = enum name (`user.Role.ToString()`).
- Former Admins (Role = 2 in the database) become Teacher (3). No database row keeps a value that would make an old account Support.
- Grading lock rule is unchanged: gradable from stage start to end date inclusive, locked from end + 1 day, using `EvaluationWindow` (Europe/Lisbon).
- Nothing in an enrollment can be graded while `Enrollment.SupervisorId` is null — for every role, Support included.
- Missing-supervisor warnings: 7 days before, 1 day before, and on/after the first stage's start date; each sent at most once per enrollment.
- Server checks are the protection; the UI only hides what a role cannot do.
- Commit messages: subject + body only, **no `Co-Authored-By` line**.
- Weights still rounded to 2 decimals; existing grades, dates and badges must not change during migration.

## Review Focus

- A browser still holding an old token (role "Admin" or "Supervisionado") must get 403 from the API and be sent back to login by the client, never a crash or a blank page → test in Task 8.
- Reassigning a student to another supervisor: the old supervisor loses access immediately, the new one gains it, existing grades stay → test in Task 3.
- Approving an extension for a stage that already locked makes it gradable again until the new date → test in Task 6.
- Teacher changes a stage's end date while an extension request is pending: the decision is validated against the end date at decision time, not the one stored in the request → test in Task 6.
- Reminder job on enrollments with no dates, archived pathways, or a supervisor assigned between warnings sends nothing (and never repeats a level) → tests in Task 7.

---

## File Structure

| File | Responsibility |
|---|---|
| `Gamp-SCPeriop.Shared/Enum/UserRole.cs` | Enum + `DisplayName()` extension (Portuguese labels) |
| `Gamp-SCPeriop.Shared/Enum/ExtensionRequestStatus.cs` | New: Pending / Approved / Rejected |
| `Gamp-SCPeriop.Shared/Data/Enrollment.cs` | + `SupervisorId`, `Supervisor`, `MissingSupervisorWarningLevel` |
| `Gamp-SCPeriop.Shared/Data/DeadlineExtensionRequest.cs` | New entity |
| `Gamp-SCPeriop.Shared/Entity/Model/ExtensionRequestDtos.cs` | New: create/decision/list DTOs |
| `Gamp-SCPeriop.Shared/Entity/Model/EnrollmentAccessDto.cs` | New: what the current user may do on an enrollment |
| `Gamp-SCPeriop.Shared/Entity/Model/SupervisorOptionDto.cs` | New: supervisor picker item |
| `GamP-SCPeriop.Server/Services/AccessService.cs` | All ownership rules (rewritten) |
| `GamP-SCPeriop.Server/Services/SupportAccountBootstrapper.cs` | New: create/promote the Support account at startup |
| `GamP-SCPeriop.Server/Services/ExtensionRequestService.cs` | New: create / decide extension requests |
| `GamP-SCPeriop.Server/Services/SupervisorReminderJob.cs` | New: one pass of missing-supervisor warnings (testable) |
| `GamP-SCPeriop.Server/Services/SupervisorReminderService.cs` | New: `BackgroundService` running the job daily |
| `GamP-SCPeriop.Server/Controllers/ExtensionRequestController.cs` | New endpoints |
| `GamP-SCPeriop.Server/Migrations/<ts>_TeacherRole.cs` | Schema + data migration |
| `GamP-SCPeriop.Tests/` | New xUnit project (InMemory EF) |
| `GamP-SCPeriop/Pages/SupervisorStudents.razor` | New: Supervisor's assigned students |
| `GamP-SCPeriop/Pages/ExtensionRequests.razor` | New: Teacher's pending requests |
| existing controllers / pages | Permission and navigation changes per task |

---

### Task 1: Test project, role enum, display names, Support bootstrap

**Files:**
- Create: `GamP-SCPeriop.Tests/GamP-SCPeriop.Tests.csproj`, `GamP-SCPeriop.Tests/TestDb.cs`, `GamP-SCPeriop.Tests/UserRoleTests.cs`, `GamP-SCPeriop.Tests/SupportAccountBootstrapperTests.cs`
- Create: `GamP-SCPeriop.Server/Services/SupportAccountBootstrapper.cs`
- Modify: `Gamp-SCPeriop.Shared/Enum/UserRole.cs`, `GamP-SCPeriop.Server/Services/AccessService.cs` (only the `Roles` class + `IsAdmin` → `IsSupport`), `GamP-SCPeriop.Server/Program.cs:153-181`, `GamP-SCPeriop.slnx`, and every file the compiler flags after the rename (server + client)

**Interfaces:**
- Produces:
  - `enum UserRole { Supervisor = 0, Student = 1, Support = 2, Teacher = 3 }`
  - `static string UserRoleExtensions.DisplayName(this UserRole role)` → "Supervisor" / "Estudante" / "Suporte" / "Professor"
  - `static class Roles { Support, Teacher, Supervisor, Student, TeacherOrSupport = "Support,Teacher", Staff = "Support,Teacher,Supervisor" }`
  - `bool ClaimsPrincipal.IsSupport()`
  - `Task<bool> SupportAccountBootstrapper.EnsureAsync(AppDbContext db, string? email, string? password, string? fullName)` → true when it created or promoted an account
  - `TestDb.Create()` → new `AppDbContext` on a unique InMemory database; `TestDb.Principal(int userId, UserRole role)` → `ClaimsPrincipal` with NameIdentifier + Role claims

- [ ] **Step 1: Create the test project**

`dotnet new xunit -n GamP-SCPeriop.Tests -f net10.0`, reference the Server and Shared projects, add `Microsoft.EntityFrameworkCore.InMemory` 10.0.10, add it to `GamP-SCPeriop.slnx`. Write `TestDb` (see Interfaces).

- [ ] **Step 2: Write the failing tests**

```csharp
[Theory]
[InlineData(UserRole.Support, "Suporte")]
[InlineData(UserRole.Teacher, "Professor")]
[InlineData(UserRole.Supervisor, "Supervisor")]
[InlineData(UserRole.Student, "Estudante")]
public void DisplayName_is_portuguese(UserRole role, string expected) => Assert.Equal(expected, role.DisplayName());

[Fact] public void Enum_values_are_stable() { Assert.Equal(0, (int)UserRole.Supervisor); Assert.Equal(1, (int)UserRole.Student); Assert.Equal(2, (int)UserRole.Support); Assert.Equal(3, (int)UserRole.Teacher); }

// SupportAccountBootstrapperTests
[Fact] public async Task Creates_support_when_none_exists()            // empty db, email+password(8+) → 1 user, Role Support, password BCrypt-verifies
[Fact] public async Task Promotes_existing_account_with_same_email()   // Teacher "x@y.pt" exists → same Id now Support, count still 1, password unchanged
[Fact] public async Task Does_nothing_when_a_support_exists()          // a Support exists → returns false, no changes
[Fact] public async Task Does_nothing_without_config_or_short_password() // null email, or password "1234567" → false
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test GamP-SCPeriop.Tests`
Expected: build errors (`UserRole.Support`, `DisplayName`, `SupportAccountBootstrapper` not defined).

- [ ] **Step 4: Implement**

Rename enum members (`Supervisionado` → `Student`, `Admin` → `Support`, add `Teacher = 3`), add `DisplayName()` in the same file, update `Roles`/`IsSupport()`, write `SupportAccountBootstrapper` (email compare case-insensitive; email match takes precedence over creating). Replace the `if (!db.Users.Any())` block in `Program.cs` with a call to it (keep the `BootstrapAdmin:*` config keys and the log messages). Then build the whole solution and fix every compile error: server `[Authorize(Roles = ...)]` that meant Admin become `Roles.TeacherOrSupport` for templates/badges/files-upload and `Roles.Support` for `AuthController.Register`, `ResetPassword` and `UserController.GetAllUsers`; client `[Authorize(Roles = "Supervisionado")]` → `"Student"`; role text shown on screen (`TopNavBarLayout`, `UserManagementTableComponent`, `DevDashboard`, `Settings`, `MyProfile`) uses `DisplayName()`. Do not change other permission logic yet (Task 3/5).

- [ ] **Step 5: Run tests and build**

Run: `dotnet test GamP-SCPeriop.Tests` → all pass. `dotnet build GamP-SCPeriop.slnx` → 0 errors.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Rename roles to Support/Teacher/Supervisor/Student and add test project"
```

---

### Task 2: Data model and migration

**Files:**
- Modify: `Gamp-SCPeriop.Shared/Data/Enrollment.cs`, `GamP-SCPeriop.Server/Data/AppDbContext.cs`
- Create: `Gamp-SCPeriop.Shared/Data/DeadlineExtensionRequest.cs`, `Gamp-SCPeriop.Shared/Enum/ExtensionRequestStatus.cs`, migration `TeacherRole`

**Interfaces:**
- Produces:
  - `Enrollment.SupervisorId : int?`, `Enrollment.Supervisor : User?`, `Enrollment.MissingSupervisorWarningLevel : int` (0 none, 1 = 7-day sent, 2 = 1-day sent, 3 = start-day sent)
  - `enum ExtensionRequestStatus { Pending = 0, Approved = 1, Rejected = 2 }`
  - `DeadlineExtensionRequest { int Id; int EnrollmentId; Enrollment? Enrollment; int ModuleId; ModuleStage Stage; int RequestedById; DateTime CurrentEndDate; DateTime RequestedEndDate; string? Reason (max 500); ExtensionRequestStatus Status; DateTime? DecidedEndDate; int? DecidedById; DateTime? DecidedAt; string? DecisionNote (max 500); DateTime CreatedAt }`
  - `AppDbContext.DeadlineExtensionRequests`

- [ ] **Step 1: Add the properties/entity and configure** — `Enrollment.Supervisor` FK to Users with `DeleteBehavior.Restrict`; `DeadlineExtensionRequest` FKs to Enrollment (Cascade), RequestedBy/DecidedBy users (Restrict, no navigation needed); index on `(EnrollmentId, ModuleId, Stage, Status)`.

- [ ] **Step 2: Create the migration**

Run: `dotnet ef migrations add TeacherRole --project GamP-SCPeriop.Server`
Then append to `Up()` after the schema operations, in this order:

```csharp
migrationBuilder.Sql("UPDATE Users SET Role = 3 WHERE Role = 2;"); // former Admins become Teachers
migrationBuilder.Sql(@"UPDATE e SET e.SupervisorId = p.ProfessorId
    FROM Enrollments e JOIN Pathways p ON p.Id = e.PathwayId JOIN Users u ON u.Id = p.ProfessorId
    WHERE u.Role = 0;"); // pathways built by supervisors: that supervisor grades its students
```

`Down()` reverses the data (`Role = 2 WHERE Role = 3`) before dropping columns.

- [ ] **Step 3: Verify the migration on a database with the old shape**

Run (scratch DB, not MyDB): create `GamP_MigrationCheck` at the previous migration (`dotnet ef database update OriginalModuleId --connection "Server=(localdb)\mssqllocaldb;Database=GamP_MigrationCheck;Trusted_Connection=True"`), insert via `sqlcmd`: one Admin (Role 2), one Supervisor (Role 0), one student, a pathway owned by the Supervisor with one enrollment, a pathway owned by the Admin with one enrollment. Then `dotnet ef database update` on the same connection.
Expected: Admin row has Role 3; enrollment in the Supervisor's pathway has `SupervisorId` = that Supervisor; the other enrollment's `SupervisorId` is NULL. Drop `GamP_MigrationCheck` afterwards.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add enrollment supervisor, extension requests table and role data migration"
```

---

### Task 3: Access rules

**Files:**
- Modify: `GamP-SCPeriop.Server/Services/AccessService.cs`
- Test: `GamP-SCPeriop.Tests/AccessServiceTests.cs`

**Interfaces:**
- Consumes: Task 1 `Roles`, `IsSupport()`, `TestDb`; Task 2 `Enrollment.SupervisorId`
- Produces (all `Task<bool>`, false when the item does not exist):
  - `CanViewStudentAsync(user, studentId)` — self; Support; Teacher with the student enrolled in one of their pathways; Supervisor with an enrollment of that student assigned to them
  - `CanManagePathwayAsync(user, pathwayId)` — Support; Teacher who owns it
  - `CanViewPathwayAsync(user, pathwayId)` — manage, or Supervisor with an assigned enrollment in it
  - `CanManageEnrollmentAsync(user, enrollmentId)` — Support; Teacher who owns the pathway (structure, dates, supervisor, extension decisions)
  - `CanGradeEnrollmentAsync(user, enrollmentId)` — enrollment has a supervisor AND (user is that supervisor OR Support)
  - `CanViewEnrollmentAsync(user, enrollmentId)` — the student, manage, or the assigned supervisor
  - `CanManageModuleAsync(user, moduleId)` / `CanManageComponentAsync(user, componentId)` — same resolution as today (pathway module → pathway; enrollment copy → enrollment), using the manage rules above; orphan modules → Support only
  - `CanViewModuleAsync(user, moduleId)` — student owning the copy, manage, assigned supervisor of the enrollment holding the copy, or `CanViewPathwayAsync` for a pathway module

- [ ] **Step 1: Write the failing tests**

Fixture: Support S, Teachers T1/T2, Supervisors V1/V2, Students A/B; pathway P1 (T1) with enrollments eA (A, supervisor V1) and eB (B, no supervisor); pathway P2 (T2) with enrollment eA2 (A, supervisor V2).

```csharp
[Fact] Teacher_manages_only_own_pathway()          // T1: P1 true, P2 false
[Fact] Supervisor_never_manages_structure()         // V1: CanManagePathway(P1) false, CanManageEnrollment(eA) false, CanManageModule(copy in eA) false
[Fact] Supervisor_grades_only_assigned()            // V1: grade eA true, eA2 false; V2: eA false
[Fact] Nobody_grades_without_supervisor()           // S, T1, V1 on eB → false
[Fact] Teacher_cannot_grade()                       // T1 grade eA false, view eA true
[Fact] Teacher_sees_only_students_in_own_pathways() // T1 view A true, B true; T2 view B false
[Fact] Supervisor_sees_only_assigned_students()     // V1 view A true, B false; view enrollment eA2 false
[Fact] Reassigning_supervisor_moves_access()        // set eA.SupervisorId = V2 → V1 grade/view eA false, V2 true
[Fact] Support_can_do_everything_but_grade_unassigned() // S: manage P1/P2, view all, grade eA true, grade eB false
[Fact] Student_sees_only_own()                      // A: view eA true, eB false, CanViewStudent(B) false
[Fact] Missing_items_return_false()                 // id 9999 for each method → false, also for Support
```

- [ ] **Step 2: Run to verify they fail** — `dotnet test GamP-SCPeriop.Tests --filter AccessServiceTests` → FAIL (new methods missing / old Supervisor-owns-pathway logic).

- [ ] **Step 3: Implement the rules listed in Interfaces** (rewrite the class's doc comments to match).

- [ ] **Step 4: Run tests** — all pass. `dotnet build` → controllers still compile (signatures of existing methods unchanged).

- [ ] **Step 5: Commit** — `git commit -m "Access rules for Teacher ownership and per-student supervisors"`

---

### Task 4: "Sem supervisor" status

**Files:**
- Modify: `GamP-SCPeriop.Server/Services/EnrollmentStatusHelper.cs` and its callers in `EnrollmentController`
- Test: `GamP-SCPeriop.Tests/EnrollmentStatusHelperTests.cs`

**Interfaces:**
- Produces: `const string SemSupervisor = "Sem supervisor"`; `GetStatus(..., bool hasSupervisor, DateTime now)` — new parameter right before `now`. Sort order: Em curso 1, Por iniciar 2, Sem supervisor 3, Pendente 4, Aprovado 5, others 6.

- [ ] **Step 1: Failing tests**

```csharp
[Fact] No_supervisor_overrides_por_iniciar_and_em_curso() // fully scheduled, start in past or future, hasSupervisor false → "Sem supervisor"
[Fact] Archived_or_fully_evaluated_still_finishes()        // isArchived true, hasSupervisor false → Aprovado/Reprovado as before
[Fact] With_supervisor_behaves_as_before()                 // existing cases with hasSupervisor true → unchanged results
[Fact] Sort_order_places_sem_supervisor_third()
```

- [ ] **Step 2: Run** → FAIL. **Step 3: Implement** (check `hasSupervisor` after the "finished" rule, before rules 2–3). Pass `e.SupervisorId != null` from every caller. **Step 4: Run** → PASS. **Step 5: Commit** — `"Show enrollments without a supervisor as 'Sem supervisor'"`

---

### Task 5: API permissions and supervisor assignment endpoints

**Files:**
- Modify: `PathwayController`, `EnrollmentController`, `ModuleController`, `ModuleComponentController`, `EvaluationController`, `UserController`, `PathwayTemplateController`, `FilesController`, `DevController` (role names only)
- Create: `Gamp-SCPeriop.Shared/Entity/Model/EnrollmentAccessDto.cs`, `SupervisorOptionDto.cs`; modify `EnrollmentDto` (+ `int? SupervisorId`), `StudentManagementDto`/`PathwayTagDto` (+ `int? SupervisorId`, `string? SupervisorName` on the per-pathway tag)
- Test: `GamP-SCPeriop.Tests/SupervisorAssignmentTests.cs`

**Interfaces:**
- Consumes: Task 3 access methods, Task 4 status.
- Produces (routes the client uses in Tasks 8–9):
  - `GET api/Pathway/owned` (TeacherOrSupport) → `List<PathwayTagDto>`: Teacher's own pathways; Support all. Replaces `api/Pathway/supervisor/{id}` (delete the old route).
  - `GET api/Pathway/{id}` → `CanViewPathwayAsync`. Create/title/delete/enrollments-list/module+component create/update/delete/timeline → `Roles.TeacherOrSupport` + manage checks. `CreatePathway`: Teacher → `ProfessorId` forced to self; Support → `ProfessorId` must be a Teacher (400 otherwise).
  - `PUT api/Pathway/{id}/teacher` body `int teacherId` (Support only) → 400 unless the user is a Teacher.
  - `GET api/Pathway/builder/{enrollmentId}` → `CanViewEnrollmentAsync` (Supervisor read-only).
  - `GET api/PathwayTemplate`, `GET api/PathwayTemplate/{id}` → TeacherOrSupport.
  - `GET api/User/supervisors` (TeacherOrSupport) → `List<SupervisorOptionDto { int Id; string FullName; string Email }>`.
  - `POST api/Enrollment` (TeacherOrSupport) accepts optional `SupervisorId`; `PUT api/Enrollment/{id}/supervisor` body `int?` (manage) — sets/clears it.
    Both: a non-null id must be a user with role Supervisor (400 "O utilizador escolhido não é um supervisor."); when the supervisor changes to a non-null value, reset `MissingSupervisorWarningLevel` to 0 and notify the new supervisor (Title "Novo estudante atribuído", Message `$"{student} no percurso \"{pathway}\" foi-lhe atribuído para avaliação."`, TargetUrl `$"/evaluate/{enrollmentId}"`). Put this in `PathwayService.AssignSupervisorAsync(int enrollmentId, int? supervisorId, int changedById)` → returns error string or null.
  - `GET api/Enrollment/assigned` (Supervisor) → enrollments where `SupervisorId == me`, shaped like `StudentManagementDto` (student, pathway, progress, status).
  - `GET api/Enrollment/management`: Support all; Teacher students of own pathways (tags only for own pathways); Supervisor → 403 (uses `assigned`). `GET api/Enrollment/supervisor/{id}` renamed to `GET api/Enrollment/owned` with the same Support/Teacher scoping.
  - `GET api/Enrollment/{id}/access` (any logged-in user with `CanViewEnrollmentAsync`) → `EnrollmentAccessDto { bool CanEdit; bool CanGrade; bool CanRequestExtension; bool HasSupervisor; string? SupervisorName }` (CanEdit = manage, CanGrade = grade, CanRequestExtension = grade).
  - `GET api/Evaluation/enrollment/{id}` → `CanViewEnrollmentAsync` for Staff. `POST api/Evaluation` → `CanGradeEnrollmentAsync`; when the enrollment has no supervisor return 400 "Esta inscrição ainda não tem supervisor atribuído: não é possível avaliar." (check before the date checks).

- [ ] **Step 1: Failing tests for `AssignSupervisorAsync`**

```csharp
[Fact] Assigns_and_notifies_new_supervisor()          // returns null, SupervisorId set, 1 notification to V1 with TargetUrl "/evaluate/{id}"
[Fact] Rejects_non_supervisor()                       // teacher id → error "O utilizador escolhido não é um supervisor.", unchanged
[Fact] Clearing_does_not_notify()                     // null → SupervisorId null, 0 notifications
[Fact] Changing_resets_warning_level()                // level 2 → assign V2 → level 0
[Fact] Same_supervisor_again_does_not_notify_twice()
```

- [ ] **Step 2: Run** → FAIL. **Step 3: Implement the service method and all endpoint changes above.** **Step 4: Run tests** → PASS; `dotnet build` → 0 errors.

- [ ] **Step 5: Commit** — `"API: Teacher owns pathways, per-student supervisor assignment, grading needs a supervisor"`

---

### Task 6: Deadline extension requests (server)

**Files:**
- Create: `GamP-SCPeriop.Server/Services/ExtensionRequestService.cs`, `GamP-SCPeriop.Server/Controllers/ExtensionRequestController.cs`, `Gamp-SCPeriop.Shared/Entity/Model/ExtensionRequestDtos.cs`
- Test: `GamP-SCPeriop.Tests/ExtensionRequestServiceTests.cs`

**Interfaces:**
- Consumes: Task 2 entity, Task 3 `CanGradeEnrollmentAsync` / `CanManageEnrollmentAsync`.
- Produces:
  - DTOs: `ExtensionRequestCreateDto { int EnrollmentId; int ModuleId; ModuleStage Stage; DateTime RequestedEndDate; string? Reason }`; `ExtensionDecisionDto { bool Approve; DateTime? EndDate; string? Note }` (EndDate null on approve = requested date); `ExtensionRequestDto { Id, EnrollmentId, StudentName, PathwayTitle, ModuleTitle, Stage, RequestedByName, CurrentEndDate, RequestedEndDate, Reason, Status, DecidedEndDate, DecisionNote, CreatedAt }`
  - `Task<(ExtensionRequestDto? Result, string? Error)> CreateAsync(ExtensionRequestCreateDto dto, int requestedById)`
  - `Task<(ExtensionRequestDto? Result, string? Error)> DecideAsync(int requestId, ExtensionDecisionDto dto, int decidedById)`
  - Routes: `POST api/ExtensionRequest` (CanGrade on the enrollment); `GET api/ExtensionRequest/pending` (TeacherOrSupport: pending requests on own pathways / all); `GET api/ExtensionRequest/enrollment/{enrollmentId}` (CanViewEnrollment); `PUT api/ExtensionRequest/{id}/decision` (CanManageEnrollment of the request's enrollment).

Rules (error texts in Portuguese, returned as 400):
- module must belong to the enrollment (`EnrollmentModules`) and have a timeline for `Stage` with an `EndDate` — "Esta fase não tem data de fim definida."
- `RequestedEndDate.Date > current EndDate.Date` — "A nova data tem de ser posterior à data de fim atual."
- no other Pending request for the same (enrollment, module, stage) — "Já existe um pedido pendente para esta fase."
- decide: request must be Pending — "Este pedido já foi decidido."; on approve the final date (dto.EndDate ?? RequestedEndDate) must be after the timeline's **current** EndDate; set timeline EndDate, Status Approved, DecidedEndDate/By/At. On reject: Status Rejected, note.
- notifications: on create → the pathway's Teacher (Title "Pedido de prolongamento", Message `$"{supervisor} pede prolongamento de \"{stageName}\" ({module}) de {student} até {date:dd/MM/yyyy}."`, TargetUrl "/extension-requests"); on decision → the requester (Title "Prolongamento aprovado" / "Prolongamento recusado", Message with the final date or the note, TargetUrl `$"/evaluate/{enrollmentId}"`). Stage names via the existing Portuguese stage labels used in `StudentEvaluation.razor` (move them to a shared helper if they are only in the page).

- [ ] **Step 1: Failing tests**

```csharp
[Fact] Create_valid_request_notifies_teacher()
[Fact] Create_rejects_date_not_after_current_end()
[Fact] Create_rejects_second_pending_for_same_stage()   // allowed again after the first is decided
[Fact] Create_rejects_module_from_another_enrollment()
[Fact] Approve_sets_requested_date_and_notifies_supervisor()
[Fact] Approve_with_other_date_uses_teacher_date()        // requested +5, teacher +3 → timeline EndDate = +3, DecidedEndDate +3
[Fact] Approve_reopens_locked_stage()                      // EndDate yesterday → after approve, EvaluationWindow.HasEnded(newEnd, today) is false
[Fact] Approve_checks_current_end_not_stored_one()         // after request, teacher moves EndDate past requested date → approve without date fails
[Fact] Reject_keeps_dates_and_stores_note()
[Fact] Cannot_decide_twice()
```

- [ ] **Step 2: Run** → FAIL. **Step 3: Implement service + controller** (register service as scoped). **Step 4: Run** → PASS.

- [ ] **Step 5: Commit** — `"Deadline extension requests: supervisor asks, teacher approves, changes date or rejects"`

---

### Task 7: Missing-supervisor reminders

**Files:**
- Create: `GamP-SCPeriop.Server/Services/SupervisorReminderJob.cs`, `GamP-SCPeriop.Server/Services/SupervisorReminderService.cs`; register in `Program.cs`
- Test: `GamP-SCPeriop.Tests/SupervisorReminderJobTests.cs`

**Interfaces:**
- Produces: `static Task<int> SupervisorReminderJob.RunAsync(AppDbContext db, DateTime today)` → number of notifications sent. `SupervisorReminderService : BackgroundService` runs it at startup and then every 24 h with `EvaluationWindow.TodayInPortugal(DateTime.UtcNow)`, in its own scope, logging and swallowing exceptions so the site keeps running.

Algorithm: for enrollments with `SupervisorId == null`, pathway not archived, and a start date = earliest `StartDate` of the stage timelines of the enrollment's module copies (skip when none): `days = (start.Date - today).Days`; `needed = days <= 0 ? 3 : days <= 1 ? 2 : days <= 7 ? 1 : 0`; when `needed > MissingSupervisorWarningLevel`, send **one** notification to the pathway's Teacher and set the level to `needed`.
- level 1/2: Title "Estudante sem supervisor", Message `$"{student} começa \"{pathway}\" a {start:dd/MM/yyyy} e ainda não tem supervisor atribuído."`
- level 3: Title "Percurso não pode começar", Message `$"O percurso \"{pathway}\" de {student} devia ter começado a {start:dd/MM/yyyy}, mas não tem supervisor atribuído."`
- TargetUrl `$"/pathway/{pathwayId}/students"`.

- [ ] **Step 1: Failing tests**

```csharp
[Fact] Sends_7_day_warning_once()            // start today+7 → 1 sent, level 1; run again same day → 0
[Fact] Sends_1_day_then_start_day()          // level 1, start today+1 → level 2; next day (start today) → level 3 with the "não pode começar" title
[Fact] Jumps_straight_to_start_level()       // level 0, start already passed → one notification, level 3
[Fact] Ignores_far_future_no_dates_archived_or_assigned() // start +20, no timelines, archived pathway, SupervisorId set → 0
```

- [ ] **Step 2: Run** → FAIL. **Step 3: Implement.** **Step 4: Run** → PASS.

- [ ] **Step 5: Commit** — `"Warn teachers about students with no supervisor before their start date"`

---

### Task 8: Client roles, navigation and read-only views

**Files:**
- Modify: `GamP-SCPeriop/Pages/LandingPage.razor`, `Dashboard.razor`, `AdminDashboard.razor`, `PathwayBuilder.razor`, `Components/PathwayBuilderComponent.razor`, `ModuleDetail.razor`, `Layout/TopNavBarLayout.razor`, `Auth/CustomAuthStateProvider.cs`
- Create: `GamP-SCPeriop/Pages/SupervisorStudents.razor`

**Interfaces:**
- Consumes: Task 5 routes (`api/Pathway/owned`, `api/Enrollment/owned`, `api/Enrollment/assigned`, `api/Enrollment/{id}/access`).

- [ ] **Step 1: Routing after login** — Support → `/admin/dashboard`; Teacher → `/dashboard`; Supervisor → `/supervisor/students`; Student → `/my-profile`.
- [ ] **Step 2: Old/unknown tokens** — in `CustomAuthStateProvider`, a token whose role claim is not a `UserRole` name (e.g. "Admin", "Supervisionado") is treated as logged out: remove it and return an anonymous state. Verify manually: put a token with role "Admin" in sessionStorage, reload → lands on the login page with no error bar.
- [ ] **Step 3: Dashboard** (`/dashboard`) → `[Authorize(Roles = "Teacher,Support")]`, uses `api/Pathway/owned` and `api/Enrollment/owned`; Support sees every pathway. `AdminDashboard` (Support) adds a card to `/dashboard`.
- [ ] **Step 4: `SupervisorStudents.razor`** at `/supervisor/students` (`[Authorize(Roles = "Supervisor")]`): list from `api/Enrollment/assigned` — student, pathway, progress, status badge — each row opens `/evaluate/{enrollmentId}`; empty state "Ainda não tem estudantes atribuídos."
- [ ] **Step 5: Read-only builder** — `PathwayBuilder` student view loads `api/Enrollment/{id}/access`; when `CanEdit` is false hide every add/edit/delete/date control in `PathwayBuilderComponent` (one `ReadOnly` parameter) and show "Só o professor pode alterar este percurso." `ModuleDetail` shows no edit controls for Supervisors.
- [ ] **Step 6: Build and check in the browser** as each role via the test panel: correct landing page, no 403 errors in the console on any page reachable from the menus.
- [ ] **Step 7: Commit** — `"Client: navigation per role, supervisor student list, read-only pathways for supervisors"`

---

### Task 9: Client supervisor assignment and extension requests

**Files:**
- Modify: `GamP-SCPeriop/Pages/PathwayStudents.razor`, `Dashboard.razor` (enroll dialog), `StudentEvaluation.razor`
- Create: `GamP-SCPeriop/Pages/ExtensionRequests.razor`

**Interfaces:**
- Consumes: `api/User/supervisors`, `PUT api/Enrollment/{id}/supervisor`, `POST api/Enrollment` with `SupervisorId`, `api/Enrollment/{id}/access`, Task 6 routes.

- [ ] **Step 1: Assignment** — in `PathwayStudents` each enrollment row shows its supervisor with a picker (from `api/User/supervisors`, plus "— Sem supervisor —"); saving calls the PUT and shows the server's error text on failure. Rows without a supervisor show a warning badge "Sem supervisor". The enroll dialogs offer an optional supervisor picker.
- [ ] **Step 2: Evaluation page** — loads `access`; when `HasSupervisor` is false show "Este estudante ainda não tem supervisor: a avaliação só pode começar depois de o professor atribuir um." and disable grading; when `CanGrade` is false (Teacher) show grades read-only. When `CanRequestExtension`, each practical stage with an end date gets "Pedir prolongamento" → small form (new end date, optional reason) → POST; the stage shows a pending/approved/rejected chip from `GET api/ExtensionRequest/enrollment/{id}`.
- [ ] **Step 3: `ExtensionRequests.razor`** at `/extension-requests` (`[Authorize(Roles = "Teacher,Support")]`): list of `pending` requests (student, pathway, module, stage, current → requested date, reason); actions **Aprovar**, **Aprovar com outra data** (date input), **Recusar** (optional note). After a decision the row leaves the list. Link to it from the Teacher dashboard with the pending count.
- [ ] **Step 4: Browser check** — as a Supervisor request an extension; as the Teacher see the notification, approve with another date; as the Supervisor see the decision notification and the stage's new end date.
- [ ] **Step 5: Commit** — `"Client: assign supervisors per student and handle extension requests"`

---

### Task 10: Test data and test panel

**Files:**
- Modify: `GamP-SCPeriop.Server/Data/DbSeeder.cs`, `GamP-SCPeriop.Server/Controllers/DevController.cs`, `Gamp-SCPeriop.Shared/Entity/Model/DevOverviewDto.cs`, `GamP-SCPeriop/Components/DevDashboard.razor`, `scripts/seed.ps1`, `scripts/seed.sh`, `README.md`

- [ ] **Step 1: Seeder** — users: 1 Support, 2 Teachers, 5 Supervisors, 20 Students (same email pattern and `DbSeeder.TestPassword`). Templates created by Teachers' side (unchanged content). Pathways owned by the 2 Teachers; each enrollment gets a Supervisor, except 2 enrollments left without one (one starting in 5 days, one already past its start); extension requests: 1 pending, 1 approved with another date, 1 rejected. Summary line and script/README texts updated to the new counts.
- [ ] **Step 2: Test panel** groups users by the four roles with `DisplayName()` labels, in the order Suporte, Professor, Supervisor, Estudante.
- [ ] **Step 3: Run** `.\scripts\seed.ps1 -Force` → summary shows the new counts; open the test panel → four groups.
- [ ] **Step 4: Commit** — `"Test data and test panel for the four roles"`

---

### Task 11: End-to-end permission check

- [ ] **Step 1:** With seeded data and the dev server running, script (scratchpad, not committed) the matrix using `api/dev/impersonate/{userId}` tokens for one user of each role, expecting:

| Call | Suporte | Professor (own / other) | Supervisor (assigned / other) | Estudante |
|---|---|---|---|---|
| `POST api/Pathway` | 200 (with a Teacher id) | 200 | 403 | 403 |
| `PUT api/Module/{copy}/timeline` | 200 | 200 / 403 | 403 | 403 |
| `PUT api/Enrollment/{id}/supervisor` | 200 | 200 / 403 | 403 | 403 |
| `POST api/Evaluation` (open stage) | 200 | 403 | 200 / 403 | 403 |
| `POST api/Evaluation` (no supervisor) | 400 | 403 | 403 | 403 |
| `GET api/Evaluation/enrollment/{id}` | 200 | 200 / 403 | 200 / 403 | 403 |
| `POST api/ExtensionRequest` | 200 | 403 | 200 / 403 | 403 |
| `PUT api/ExtensionRequest/{id}/decision` | 200 | 200 / 403 | 403 | 403 |
| `POST api/PathwayTemplate`, `POST api/badgetemplate` | 200 | 200 | 403 | 403 |
| `POST api/Auth/register` | 200 | 403 | 403 | 403 |
| any call with an old "Admin" token | 403 | — | — | — |

- [ ] **Step 2:** `dotnet test GamP-SCPeriop.Tests` → all pass; `dotnet build GamP-SCPeriop.slnx -c Release` → 0 errors.
- [ ] **Step 3:** Fix anything that fails in the task that owns it, then report the matrix result.
