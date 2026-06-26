namespace ExamPlatform.BLL.Exceptions
{
    public class CertificateNotFoundException : BusinessException
    {
        public CertificateNotFoundException(int certId)
            : base($"Certificate with id {certId} not found.") { }
    }
}