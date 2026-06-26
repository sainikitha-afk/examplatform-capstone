namespace ExamPlatform.DAL.Interfaces
{
    // ───────────── THE UNIVERSAL CRUD CONTRACT ─────────────
    //
    // This is the heart of the Repository Pattern: ONE interface that
    // describes the CRUD operations every entity supports.
    //
    // Generic type parameters:
    //   K — the type of the entity's primary key (int, string, Guid, etc.)
    //   T — the entity type itself (User, Exam, Question, etc.)
    //
    // Constraint: 'where T : class'
    //   Restricts T to reference types only (entities are always classes
    //   in our system, never structs). This is required because EF Core
    //   methods like Set<T>() also constrain T to class.
    //
    // Why 'I' prefix? .NET convention for interfaces. Recognizable at a glance.
    public interface IRepository<K, T> where T : class
    {
        // ───────────── READ ─────────────
        // Task<T?> — async return. T? means "may be null" (if not found).
        // This is the read-by-key operation. Every entity supports it.
        Task<T?> GetByIdAsync(K id);

        // Returns all rows of T. IEnumerable<T> (lazy) vs List<T> (materialized).
        // We return IEnumerable to leave the caller flexible — they can ToList,
        // foreach, or further filter without us forcing materialization.
        Task<IEnumerable<T>> GetAllAsync();

        // ───────────── WRITE ─────────────
        // AddAsync: enqueue an insert in the change tracker. Doesn't hit DB
        // until SaveChangesAsync() is called. Returns the entity (now with
        // its DB-generated ID if applicable).
        Task<T> AddAsync(T entity);

        // Update: mark the entity as modified in the change tracker.
        // Not async (no DB call here — just flags the entity).
        T Update(T entity);

        // Delete: mark the entity as deleted in the change tracker.
        // We pass the FULL entity (already loaded), not just the ID.
        // For delete-by-id, the service would call GetByIdAsync first.
        T Delete(T entity);

        // ───────────── PERSIST ─────────────
        // The Unit-of-Work commit. Calls DbContext.SaveChangesAsync().
        // Returns rows affected count.
        //
        // Why is this in the repository? Because the repository owns the
        // DbContext reference. Services don't see DbContext.
        Task<int> SaveChangesAsync();

        // ───────────── PAGINATION ─────────────
        // Returns a page of T alongside the total row count.
        // Returns a value-tuple (not PagedResult<T>) to avoid a cross-layer
        // dependency — DAL must not reference BLL.Models.
        // Services convert the tuple into PagedResult<T>.
        //
        // page     — 1-based page number.
        // pageSize — items per page; caller is responsible for clamping to [1,100].
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
    }
}