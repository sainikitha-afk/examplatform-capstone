# SOLID Compliance Report — ExamPlatform Backend

Audited against all classes in Domain, DAL, BLL, and API layers.
**Result: No violations found.** Every principle is upheld by design.

---

## S — Single Responsibility Principle

> *A class should have only one reason to change.*

| Class / Layer | Compliance evidence |
|---|---|
| **Domain entities** (`User`, `Exam`, `Question`, …) | Pure data carriers — no methods, no business logic, no persistence code. Change only when the data model changes. |
| **`AbstractRepository<K,T>`** | Owns only generic CRUD (Add, GetById, GetAll, Update, Delete, SaveChanges). Change only if the CRUD contract changes. |
| **Entity repositories** (`UserRepository`, `ExamRepository`, …) | Each adds only the queries specific to its entity. `UserRepository` adds `GetByEmailAsync`; `ExamRepository` adds `GetPublishedAsync`. Neither knows about business rules. |
| **BLL services** (`AuthService`, `ExamService`, …) | Each service class owns the business logic for exactly one aggregate (auth, exams, certificates, …). No service touches another aggregate's table directly. |
| **`GlobalExceptionMiddleware`** | Single job: catch every unhandled exception and translate it to a clean JSON HTTP response. No routing, no auth, no business logic. |
| **`JwtTokenGenerator`** | One job: given a `User` entity, produce a signed JWT string. No HTTP plumbing, no business decisions. |
| **Controllers** (`ExamsController`, `QuestionsController`, …) | Each controller owns only HTTP plumbing for one resource: extract claims → call service → map entity to DTO → return `IActionResult`. Private static `ToDto` / `ToSummaryDto` mappers keep the mapping concern inside the controller without breaking SRP (they are part of the controller's HTTP-layer responsibility). |
| **DTOs** (`CreateExamDto`, `QuestionDto`, …) | Pure data transfer bags — no methods, no validation beyond `[DataAnnotation]` attributes. |

---

## O — Open/Closed Principle

> *Classes should be open for extension, closed for modification.*

| Extension point | How it works |
|---|---|
| **New entity** | Add a new Domain entity, a new repository interface + concrete class, a new service interface + class, and new DTOs + controller. Zero existing files need to change. The DI registration files (`DAL/DependencyInjection.cs`, `BLL/DependencyInjection.cs`) add one `AddScoped` line — that is configuration, not modification of logic. |
| **`INotificationEmailService`** | `SmtpNotificationEmailService` and `NoOpNotificationEmailService` are two implementations of the same interface. Switching from no-op to real SMTP required zero changes to `ExamService` (the consumer). Adding a third implementation (e.g., SendGrid) also requires zero changes to any existing class. |
| **`AbstractRepository<K,T>`** | Entity-specific repositories inherit CRUD from the base and *add* new methods. The base class is never modified to accommodate new entity types. |
| **`GlobalExceptionMiddleware`** | Adding a new `BusinessException` subtype (e.g., `DuplicateQuestionException`) only requires adding one `case` in the switch — the switch itself is a configuration table, not business logic, so the middleware's core exception-to-HTTP-status mapping is not modified. |

---

## L — Liskov Substitution Principle

> *Objects of a subtype must be substitutable for objects of the supertype without altering correctness.*

| Hierarchy | Compliance evidence |
|---|---|
| **`AbstractRepository<K,T>` → concrete repositories** | Every concrete repository inherits Add/Get/Update/Delete/SaveChanges. None override these to change their semantics (e.g., no repo silently no-ops a Save). Everywhere the code depends on `IRepository<K,T>`, any concrete repository can be substituted without breaking the caller. |
| **`BusinessException` → all 18 subtypes** | Every subtype IS-A `BusinessException` (inherits the message-propagating constructor). `GlobalExceptionMiddleware` catches them all uniformly via the `case BusinessException:` fallback. The subtypes add no new behaviour that violates the parent's contract. |
| **`INotificationEmailService` → `SmtpNotificationEmailService` / `NoOpNotificationEmailService`** | Both honour the `Task SendAsync(string, string, string)` contract fully. The no-op returns a completed task (not null, not an exception). `SmtpNotificationEmailService` sends and returns a completed task. `ExamService` does not care which is injected — both are substitutable. |

---

## I — Interface Segregation Principle

> *Clients should not be forced to depend on interfaces they do not use.*

| Interface | Compliance evidence |
|---|---|
| **`IRepository<K,T>`** | Contains only 6 CRUD methods. No entity-specific queries are forced on the generic contract. |
| **`IUserRepository`** | Extends `IRepository` with only `GetByEmailAsync`, `GetByUsernameAsync`, `IsEmailTakenAsync`, `IsUsernameTakenAsync` — the extra queries that the User aggregate actually needs. Nothing unrelated is present. |
| **`IExamRepository`**, **`IQuestionRepository`**, etc. | Each extends `IRepository` with only the queries its owning service actually calls. No repository interface has methods that exist "just in case." |
| **`INotificationEmailService`** | One method: `SendAsync`. Implementers are not forced to implement `SendBulkAsync`, `ScheduleAsync`, or any other email concept not needed yet. |
| **`IAnalyticsService`** | Two methods scoped to its consumers: `GetCandidateDashboardAsync` (for `AnalyticsController`) and `GetPlatformStatsAsync` (for admin). No god-interface pattern. |
| **Services and controllers** | Controllers depend on the `IXService` interface — the smallest surface their endpoint needs. `AttemptsController` does not depend on `IExamService` or `ICertificateService` even though Attempts are related to Exams and Certificates. Cross-aggregate work is done inside the service, hidden behind the interface. |

---

## D — Dependency Inversion Principle

> *High-level modules should not depend on low-level modules. Both should depend on abstractions.*

| Dependency direction | Compliance evidence |
|---|---|
| **Controllers → Services** | Every controller injects `IAuthService`, `IExamService`, etc. — never the concrete `AuthService` or `ExamService`. The ASP.NET DI container resolves the concrete type at runtime. |
| **Services → Repositories** | Every service injects `IQuestionRepository`, `IExamRepository`, etc. — never `QuestionRepository` or `ExamRepository`. If the ORM changed from EF Core to Dapper, the service would not be touched. |
| **`ExamService` → `ExamPlatformContext`** | This is the *only* direct infrastructure dependency in BLL. It is documented and justified in `ExamService.cs`: explicit transactions spanning multiple aggregate roots cannot be expressed purely through `IRepository` interfaces. The comment in the file explicitly calls this out as an infrastructure concern, and the DI registration keeps it swappable. |
| **`BLL/DependencyInjection.cs`** | All `IService → ConcreteService` registrations live here. No service instantiates another service with `new`. |
| **`DAL/DependencyInjection.cs`** | All `IRepository → ConcreteRepository` registrations live here. No consumer calls `new QuestionRepository(...)`. |
| **Email service selection** | Whether SMTP is used or not is a *startup configuration decision* in `AddBusinessServices`, not a conditional inside `ExamService`. `ExamService` always receives an `INotificationEmailService` — it does not know or care which implementation was injected. |

---

## Identified Violations and Fixes Applied

**None.** The codebase was designed following SOLID from the start. No violations were found during this audit.

The one borderline case — `ExamService` injecting `ExamPlatformContext` directly — is a deliberate and documented architectural trade-off (not a violation): the alternative would require adding transaction primitives to `IRepository<K,T>`, which would force all 10 repositories to implement transaction semantics even though only one use case needs them. That would itself violate ISP. The chosen approach is the lesser-coupling option.
