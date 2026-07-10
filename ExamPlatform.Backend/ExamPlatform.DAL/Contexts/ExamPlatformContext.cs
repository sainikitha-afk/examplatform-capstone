// EF Core namespace — gives us DbContext, DbSet<T>, ModelBuilder, etc.
using Microsoft.EntityFrameworkCore;

// We're going to expose DbSet<T> for every domain entity, so we import them.
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Contexts
{
    // ExamPlatformContext IS our DbContext — the single class that represents
    // an EF Core session with the PostgreSQL database.
    //
    // Inheriting DbContext gives us:
    //   - SaveChanges() / SaveChangesAsync()  → persist tracked changes
    //   - ChangeTracker                       → which entities were modified
    //   - Database property                   → low-level access (transactions, raw SQL)
    //   - Set<T>()                            → generic access to a DbSet
    //   - OnConfiguring / OnModelCreating     → hooks for setup
    public class ExamPlatformContext : DbContext
    {
        // ───────────── CONSTRUCTOR INJECTION ─────────────
        //
        // DbContextOptions<ExamPlatformContext> is a bag of configuration:
        // connection string, which provider (Npgsql), logging, etc.
        //
        // We DON'T construct it ourselves. The DI container in API will build
        // it from appsettings.json and HAND it to us. This is constructor
        // injection — the foundation of testability and loose coupling.
        //
        // ': base(options)' forwards the options to the parent DbContext
        // class, which uses them to wire up the provider and connection.
        public ExamPlatformContext(DbContextOptions<ExamPlatformContext> options)
            : base(options)
        {
        }

        // ───────────── DbSet PROPERTIES ─────────────
        //
        // Each DbSet<TEntity> represents ONE table. EF Core uses these to:
        //   - Know which tables exist (10 DbSets → 10 tables in migration)
        //   - Pluralize the property name to derive the table name by
        //     convention (Users → "users", ExamCategories → "exam_categories"
        //     — with our snake_case configuration coming in a later step)
        //   - Generate type-safe query roots: _context.Users.Where(...)
        //
        // DbSet<T> implements IQueryable<T>, so LINQ works directly against
        // it. Calling .Where()/.Select() builds an expression tree; EF Core
        // translates that tree into SQL at execution time.

        public DbSet<User> Users => Set<User>();
        public DbSet<ExamCategory> ExamCategories => Set<ExamCategory>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
        public DbSet<Attempt> Attempts => Set<Attempt>();
        public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();
        public DbSet<Certificate> Certificates => Set<Certificate>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<ExaminerRequest> ExaminerRequests => Set<ExaminerRequest>();
        public DbSet<IssueReport> IssueReports => Set<IssueReport>();

        // Note on syntax: "=> Set<T>()" is an expression-bodied property,
        // equivalent to "{ get { return Set<T>(); } }". Some codebases use
        // "DbSet<User> Users { get; set; } = null!;" instead. Both work;
        // Set<T>() is slightly more explicit about what's happening (it
        // asks the context for the DbSet by type) and is preferred in newer
        // EF Core docs.

        // ───────────── MODEL CONFIGURATION ─────────────
        //
        // OnModelCreating is the hook EF Core calls ONCE at startup, after
        // it scans your entities by convention, to let you override or
        // extend the discovered model.
        //
        // We will keep this method tiny: it just tells EF Core to find ALL
        // IEntityTypeConfiguration<T> classes in this assembly and apply
        // them. Each entity's config will live in its own file under
        // Configurations/. The actual configuration code lives in those
        // files — this method stays clean even when we have 50 entities.
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Run all IEntityTypeConfiguration<T> classes in this assembly.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExamPlatformContext).Assembly);

            // ───────────── GLOBAL NAMING CONVENTION ─────────────
            // After all configs run, EF Core knows the model. We now walk every
            // entity and rewrite its table name, column names, key names, index
            // names, and FK names into snake_case. This single block saves us
            // 80+ HasColumnName calls across the configs and keeps schema style
            // consistent with PostgreSQL conventions.
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Table name: "ExamCategories" → "exam_categories"
                var tableName = entity.GetTableName();
                if (tableName != null)
                    entity.SetTableName(ToSnakeCase(tableName));

                // Column names for every property
                foreach (var property in entity.GetProperties())
                    property.SetColumnName(ToSnakeCase(property.Name));

                // Primary/alternate key constraint names
                foreach (var key in entity.GetKeys())
                    key.SetName(ToSnakeCase(key.GetName()!));

                // Foreign key constraint names
                foreach (var fk in entity.GetForeignKeys())
                    fk.SetConstraintName(ToSnakeCase(fk.GetConstraintName()!));

                // Index names
                foreach (var index in entity.GetIndexes())
                    index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
            }
        }

        // Local helper: converts PascalCase / camelCase → snake_case.
        // "ExamCategories" → "exam_categories", "PassingMarks" → "passing_marks".
        private static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                // Insert underscore before each uppercase letter (except the first char)
                if (char.IsUpper(c) && i > 0 && !char.IsUpper(input[i - 1]))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
        
    }
}