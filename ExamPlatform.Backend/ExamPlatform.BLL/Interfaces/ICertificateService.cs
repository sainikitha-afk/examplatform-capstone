using ExamPlatform.BLL.Models;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.BLL.Interfaces
{
    public interface ICertificateService
    {
        // Admin: list all certificates platform-wide.
        Task<IEnumerable<Certificate>> GetAllAsync();

        // Admin: paginated list of all certificates platform-wide.
        Task<PagedResult<Certificate>> GetAllPagedAsync(int page, int pageSize);

        // Admin or candidate: get one cert with User + Exam included.
        // Caller decides who can see it — the controller enforces roles.
        Task<Certificate> GetByIdAsync(int certificateId);

        // ─── CANDIDATE ENDPOINTS ─────────────────────────────────────

        // Candidate: list of all certificates belonging to this candidate.
        // Service enforces no cross-user visibility — only certs where
        // cert.UserId == candidateId are returned.
        Task<IEnumerable<Certificate>> GetByUserAsync(int candidateId);

        // Candidate or Admin: load a single certificate with ownership enforcement.
        //   isAdmin=true  → any cert can be loaded (admin sees all)
        //   isAdmin=false → cert.UserId must equal callerId, otherwise 404
        // Returning 404 (not 403) when the candidate requests someone else's cert
        // avoids leaking the existence of certificates to unauthorised callers.
        Task<Certificate> GetByIdWithDetailsAsync(int certId, int callerId, bool isAdmin);

        // Admin: revoke a certificate and notify the candidate.
        // Doesn't delete the cert row — flips CertStatus to Revoked and sends
        // a CertificateRevoked in-app notification to the certificate's owner.
        Task<Certificate> RevokeAsync(int certificateId);
    }
}