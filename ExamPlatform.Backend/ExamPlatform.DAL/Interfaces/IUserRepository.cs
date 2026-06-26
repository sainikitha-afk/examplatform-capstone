using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Interfaces
{
    // IUserRepository INHERITS the generic IRepository contract AND
    // adds operations that only make sense for User.
    //
    // Why inherit instead of having two separate interfaces? Because a
    // concrete UserRepository needs to expose BOTH the generic CRUD and
    // the user-specific operations. One inherited interface = one type
    // for DI to register and inject.
    public interface IUserRepository : IRepository<int, User>
    {
        // Auth needs to look up users by email AND username (the user can
        // log in with either, typically). These are non-key lookups, so
        // FindAsync doesn't help — we need .Where() over indexed columns.
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);

        // Existence checks for registration validation. Returning bool is
        // more efficient than fetching the full user just to check existence.
        Task<bool> IsEmailTakenAsync(string email);
        Task<bool> IsUsernameTakenAsync(string username);
    }
}