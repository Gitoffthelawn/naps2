using System.Net.Http;
using System.Security.Authentication;
using NAPS2.Escl;
using NAPS2.Scan;
using NAPS2.Sdk.Tests.Asserts;
using Xunit;
using Xunit.Abstractions;

namespace NAPS2.Sdk.Tests.Remoting;

public class SelfSignedCertScanServerTests(ITestOutputHelper testOutputHelper)
    : ScanServerTestsBase(testOutputHelper, EsclSecurityPolicy.RequireHttps)
{
    [NetworkFact(Timeout = TIMEOUT)]
    public async Task FindDevice()
    {
        Assert.True(await TryFindClientDevice());
    }

    [NetworkFact(Timeout = TIMEOUT)]
    public async Task Scan()
    {
        _bridge.MockOutput = CreateScannedImages(ImageResources.dog);
        var images = await _client.Scan(new ScanOptions
        {
            Device = _clientDevice,
            EsclOptions =
            {
                SecurityPolicy = EsclSecurityPolicy.RequireHttps
            }
        }).ToListAsync();
        Assert.Single(images);
        ImageAsserts.Similar(ImageResources.dog, images[0]);
    }

    [NetworkFact(Timeout = TIMEOUT)]
    public async Task ScanPreventedByTrustedCertificateSecurityPolicy()
    {
        var scanResult = _client.Scan(new ScanOptions
        {
            Device = _clientDevice,
            EsclOptions =
            {
                SecurityPolicy = EsclSecurityPolicy.RequireTrustedCertificate
            }
        });
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await scanResult.ToListAsync());
        Assert.True(exception.InnerException is AuthenticationException ||
                    exception.InnerException?.InnerException is AuthenticationException);
    }
}