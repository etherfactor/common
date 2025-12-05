namespace EtherGizmos.Common.Configuration;

public class RootCertificateOptions : KeyOptions
{
    public PfxFileCertificateOptions? PfxFile { get; set; }

    public PfxRawCertificateOptions? PfxRaw { get; set; }

    public PfxSplitRawCertificateOptions? PfxSplitRaw { get; set; }
}
