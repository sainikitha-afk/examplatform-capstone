using Microsoft.EntityFrameworkCore;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;

namespace ExamPlatform.DAL.Repositories
{
    // ───────────── THE COMMON IMPLEMENTATION ─────────────
    //
    // 'abstract' — this class cannot be instantiated directly.
    // It exists ONLY to be inherited by concrete repositories. That's
    // a deliberate design signal: "you're not supposed to use this directly."
    //
    // The class implements IRepository<K, T> once, providing the universal
    // CRUD logic. Every concrete repo (UserRepository, ExamRepository, etc.)
    // inherits these implementations for free.
    public abstract class AbstractRepository<K, T> : IRepository<K, T>
        where T : class
    {
        // ───────────── DBCONTEXT DEPENDENCY ─────────────
        // 'protected' so child classes can use it directly when they need
        // entity-specific queries that go beyond the generic CRUD.
        // 'readonly' because once injected, it must not be reassigned.
        protected readonly ExamPlatformContext _context;

        // The DbSet<T> for this entity type. Cached as a field so we don't
        // call _context.Set<T>() repeatedly. EF Core returns the same DbSet
        // instance regardless, but the field is more readable.
        protected readonly DbSet<T> _dbSet;

        // Constructor injection. When DI creates a UserRepository (which
        // inherits AbstractRepository<int, User>), DI provides the DbContext.
        // Through the 'base' constructor, that DbContext flows up here.
        protected AbstractRepository(ExamPlatformContext context)
        {
            _context = context;

            // Set<T>() asks the DbContext for the DbSet of type T.
            // It's how the generic class accesses the right table without
            // hardcoding which DbSet property to use.
            _dbSet = context.Set<T>();
        }

        // ───────────── READ ─────────────

        // FindAsync is the EF Core method optimized for primary-key lookups.
        // It first checks the change tracker (for entities already loaded)
        // and only queries the DB if not found there. Faster than .Where(...)
        // for PK lookups, but ONLY works with the actual PK.
        //
        // The 'object[]' wrap is because FindAsync supports composite keys —
        // we pass our single key K wrapped in an array.
        public virtual async Task<T?> GetByIdAsync(K id)
        {
            // The '!' on id silences a nullable warning — K is a generic type
            // parameter; the compiler doesn't know it's non-null. We trust
            // the caller to pass a real key.
            return await _dbSet.FindAsync(id!);
        }

        // ToListAsync() executes the query, returning every row as a List.
        // For large tables, callers should prefer entity-specific methods
        // with .Where(...) filters — this method is for small reference tables.
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        // ───────────── WRITE ─────────────

        // AddAsync queues an INSERT in the change tracker.
        // It does NOT hit the database — the actual INSERT happens on
        // SaveChangesAsync. The entity is returned, and after SaveChanges
        // EF Core back-fills its database-generated PK.
        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        // Update marks the entity as modified. EF Core's change tracker
        // knows what fields changed since it was loaded. SaveChanges issues
        // an UPDATE for only those fields.
        public virtual T Update(T entity)
        {
            _dbSet.Update(entity);
            return entity;
        }

        // Delete marks the entity for removal. SaveChanges issues a DELETE.
        // For soft-deletes (like our Question), services should flip the
        // IsActive flag via Update() instead of calling this method.
        public virtual T Delete(T entity)
        {
            _dbSet.Remove(entity);
            return entity;
        }

        // ───────────── PERSIST ─────────────

        // The Unit-of-Work commit. Returns the count of affected rows.
        // EF Core wraps multiple pending changes in one transaction by default,
        // so this method atomically commits everything queued since the last
        // SaveChanges call.
        public virtual async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // ───────────── PAGINATION ─────────────

        // Default implementation: applies Skip/Take to the full _dbSet in a single
        // database round-trip. CountAsync and ToListAsync are two separate SQL calls
        // (COUNT(*) + SELECT ... LIMIT/OFFSET) because EF Core translates both to
        // parameterised queries.
        //
        // Repositories with filtered queries (e.g. ExamRepository.GetPublishedAsync)
        // override or add paginated variants that apply WHERE before Skip/Take.
        //
        // AsNoTracking: pagination reads are read-only — no need to track entities
        // in the change tracker, which saves memory for large result sets.
        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize)
        {
            // _dbSet.AsNoTracking() returns IQueryable<T>; no SQL sent yet.
            var query = _dbSet.AsNoTracking();

            // COUNT(*) — one SQL round-trip.
            var totalCount = await query.CountAsync();

            // SELECT ... SKIP/TAKE — one SQL round-trip.
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}