using Codecepticon.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Codecepticon.Modules.Sign
{
    class CertificateManager
    {
        private const int KeyLength = 2048;

        public bool GenerateCertificate(string Subject, string Issuer, DateTime NotBefore, DateTime NotAfter, string Password, string PfxOutput)
        {
            // https://stackoverflow.com/a/48210587/2445959
            RSA keyPair = RSA.Create(KeyLength);

            Logger.Verbose("Generating issuer certificate...");
            Logger.Verbose("Issuer is " + Issuer);
            X509Certificate2 certificateIssuer = GenerateIssuerCertificate(Issuer, NotBefore, NotAfter);

            Logger.Info("Generating signing certificate...");
            Logger.Verbose("Subject is " + Subject);
            CertificateRequest certRequest = new(Subject, keyPair, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            certRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            //certRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.8") }, true));
            certRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(certRequest.PublicKey, false));
            X509Certificate2 cert = certRequest.Create(certificateIssuer, NotBefore, NotAfter, new byte[] { 1, 2, 3, 4 });

            // Add the private key back to the certificate.
            X509Certificate2 certificate = cert.CopyWithPrivateKey(keyPair);

            Logger.Info("Exporting certificate to file...");
            File.WriteAllBytes(PfxOutput, certificate.Export(X509ContentType.Pfx, Password));
            
            return true;
        }

        private X509Certificate2 GenerateIssuerCertificate(string Issuer, DateTime NotBefore, DateTime NotAfter)
        {
            RSA keyPair = RSA.Create(KeyLength);

            CertificateRequest issuerRequest = new(Issuer, keyPair, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            issuerRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            issuerRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(issuerRequest.PublicKey, false));
            return issuerRequest.CreateSelfSigned(NotBefore, NotAfter);
        }

        public bool CheckPfxPassword(string pfxFile, string password)
        {
            try
            {
                X509Certificate2 certificate = new(pfxFile, password);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        public X509Certificate GetCertificateFromFile(string signedFile)
        {
            return X509Certificate.CreateFromSignedFile(signedFile);
        }
    }
}
