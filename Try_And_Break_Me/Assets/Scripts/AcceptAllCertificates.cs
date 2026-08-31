using UnityEngine.Networking;

// Some Unity versions/platforms reject perfectly valid HTTPS certificates (e.g. Render's), which
// surfaces as "Cannot connect to destination host" even though a browser reaches the server fine.
// Attaching this handler to a UnityWebRequest tells Unity to accept the certificate so the request
// completes. Fine for a hosted relay you control / a student project; don't ship it for handling
// sensitive third-party traffic in production.
public class AcceptAllCertificates : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData) => true;
}