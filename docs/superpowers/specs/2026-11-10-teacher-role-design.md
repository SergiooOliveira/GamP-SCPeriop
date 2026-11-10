# Teacher role and role restructure — design

## Why

Today the app has three roles: Admin, Supervisor and Supervisionado (student). "Admin" was never really an
administrator: it is the professor who stays at the school. And the Supervisor (the internship supervisor) currently
builds pathways, enrolls students and sets dates, which is really the school professor's job.

The real-world structure is:

- the **school professor** (Teacher) designs the learning pathway, decides who is in it and who supervises each
  student, and sets the evaluation periods;
- the **internship supervisor** works with the student on placement and grades them;
- someone (the developer, for now) needs a **support** account that can see and fix everything to help users.

This change makes the roles match that structure.

## Roles

Shown on screen in Portuguese as **Suporte**, **Professor**, **Supervisor**, **Estudante**. Code stays in English.

| Role | On screen | Who |
|---|---|---|
| Support | Suporte | The developer / support person. Top level. |
| Teacher | Professor | The school professor. Today's "Admin" accounts become this. |
| Supervisor | Supervisor | The internship supervisor. |
| Student | Estudante | Unchanged (today "Supervisionado"). |

### What each role can do

**Support (Suporte)**
- Everything, everywhere: every pathway, student, grade, template, badge and account. The purpose is to help
  Teachers and Supervisors when something goes wrong.
- Creates accounts (for now the only way accounts are created; later they will come from a sync with the school's
  system — not part of this work).
- Can change which Teacher owns a pathway (needed to fix up existing data, see Migration).

**Teacher (Professor)**
- Manages pathway templates, module/component templates and badge templates (moved from today's Admin).
- Creates pathways from templates. Is the only role (besides Support) that can change a pathway's structure:
  modules, parameters, sub-parameters, weights, documents, order.
- Enrolls students in their pathways and assigns **one supervisor per student enrollment** (two students in the same
  pathway may have different supervisors). Can change or remove the assigned supervisor later.
- Sets the evaluation dates (start/end of each stage) — taking this over from the Supervisor.
- Sees only **their own students**: a student is a Teacher's student when the student is enrolled in a pathway that
  Teacher owns (`Pathway.ProfessorId`). If a student is in pathways of two Teachers, each Teacher sees that student
  but only the enrollments in their own pathways.
- Sees their students' grades and progress (read-only), so they can adapt the pathway. Does **not** grade: grading is only the Supervisor's job.
- Receives and decides extension requests (see below).
- Cannot create accounts.

**Supervisor**
- Sees only the students assigned to them (enrollments where they are the assigned supervisor), and only those
  enrollments.
- Grades those students, with the existing lock rule unchanged: a stage can be graded and re-graded from its start
  date to its end date inclusive, and locks from end date + 1 day (Europe/Lisbon).
- Can no longer create pathways, change pathway structure, enroll students or change dates. The pathway/module
  screens are read-only for them, apart from grading.
- Can request a deadline extension for a stage of a student they supervise.

**Student (Estudante)**
- Unchanged: sees only their own pathways, grades and badges.

## Deadline extension requests

1. On a student's evaluation page, for a stage, the Supervisor clicks "Pedir prolongamento", picks the end date they
   want and may write a short reason.
2. The Teacher who owns the pathway gets a notification linking to the request.
3. The Teacher can:
   - **Approve** — the stage's end date becomes the requested date;
   - **Approve with another date** — the Teacher picks a different end date (e.g. 3 days instead of 5);
   - **Reject** — optionally with a short reason.
4. The Supervisor gets a notification with the decision.
5. Approving changes that student's stage end date (dates are per student, because each enrollment has its own copy
   of the modules and stage timelines). A stage that had already locked becomes gradable again until the new end date.

Rules (builder may refine):
- Only the assigned Supervisor (or Support) can request; only the owning Teacher (or Support) can decide.
- At most one pending request per student stage; the Supervisor sees that a request is pending and its outcome.
- A request can be made before or after the stage ended (a supervisor often notices late). The new date must be after
  the current end date.
- The Teacher can also see all pending requests for their pathways in one list (e.g. a section on their dashboard),
  not only through the notification.

## Data changes

- `UserRole`: Support, Teacher, Supervisor, Student. Role names in the JWT change accordingly.
  - Renaming "Admin" means old tokens that say "Admin" no longer grant anything — important, because former Admins
    become Teachers and must not keep full rights through an old token. Everyone logs in again after the deploy.
- `Enrollment.SupervisorId` (nullable, user with role Supervisor).
- New table for extension requests: enrollment, module, stage, requested by, current end date, requested end date,
  reason, status (pending / approved / rejected), decided end date, decided by, decided at, created at.

## Migration of existing data (live server)

- Every user with role Admin becomes Teacher.
- A Support account is created for the developer: on startup, if no Support user exists and
  `BootstrapAdmin:Email`/`Password` are configured, create it (today this only runs on an empty database).
  If an account with that email already exists, promote it to Support instead of creating a duplicate.
- Existing pathways: today `ProfessorId` may point to a Supervisor (supervisors created pathways). For each existing
  enrollment whose pathway owner is a Supervisor, set `Enrollment.SupervisorId` to that Supervisor. The pathway's
  owner is then fixed by Support through the "change Teacher" action. (Live data is tiny: 2 pathways, 7 users.)
- Existing stage dates, grades and badges are kept as they are.

## Other affected parts

- Every API permission check (controllers + `AccessService`) and every role check in the Blazor pages, following
  the table above. Server checks are the real protection; the UI only hides what the user cannot do.
- Navigation/dashboards: Teacher gets today's admin areas (templates, badges) plus their pathways; Supervisor gets a
  list of their assigned students; Support sees everything plus account management.
- Test data (`DbSeeder`): 1 Support, 2 Teachers, 5 Supervisors, 20 Students. Pathways owned by Teachers, each
  enrollment with an assigned Supervisor, a couple of extension requests in each state.
- Dev dashboard: shows the four roles with their Portuguese names.

## Non-goals

- Syncing accounts from the school's system.
- Teachers creating accounts.
- Several Teachers sharing one pathway.
- Teachers grading, or any change to the grading/lock rules themselves.
- Changing what students see.

## Anti-goals (would be a failure even if it "works")

- A former Admin keeping full access after the change.
- A Supervisor able to change a pathway's structure or dates by calling the API directly.
- A Teacher or Supervisor seeing students that are not theirs.
- Losing or altering existing grades and dates on the live server.

## Enrollments without a supervisor

- Assigning a supervisor is optional when enrolling; a student can be enrolled first and get a supervisor later.
  Until then only the Teacher (and Support) see that enrollment.
- An enrollment **cannot start without a supervisor**: while none is set it shows as "Sem supervisor" (instead of
  "Por iniciar"/"Em curso") and nothing in it can be graded, even if its dates have arrived.
- The Teacher is notified about enrollments without a supervisor:
  - as the start date gets close: 7 days before and again 1 day before the first stage's start date (builder's
    call on the exact days; a daily background check on the server);
  - on the start date itself, if still no supervisor, saying the student's pathway could not start.
  Each warning is sent once per enrollment, not every day.
- The Teacher's student list clearly marks enrollments without a supervisor.
- The Supervisor gets a notification when a student is assigned to them.

## Calls made while writing this (approved)

- A Supervisor can still open the pathway and module pages of their students' pathways, read-only.

## Left to the builder

- Exact screens and layout for assigning supervisors, the request button/dialog and the Teacher's request list,
  following the existing look of the app.
- Whether "Teacher sets dates" stays per student (as the data is today) or also gets a "apply to all students of
  this pathway" shortcut — per student is the minimum.
- How the role rename is done in the EF migration (keep enum numbers stable where possible).
