using Microsoft.Extensions.Configuration;
using NSubstitute;
using SecurityCamera.Infrastructure.UltralyticsAi;

namespace SecurityCamera.Infrastructure.Tests;

public class UltralyticsAiServiceTests
{
    UltralyticsAiService _sut;
    [SetUp]
    public void Setup()
    {
        IConfiguration configuration = Substitute.For<IConfiguration>();
        _sut = new UltralyticsAiService(configuration);
    }

    [Test]
    public async Task AnalyseImage()
    {
        Domain.ImageRecorderDomain.Events.ImageRecordedEvent imageRecordedEvent = new(
            OccurrenceDateTime: DateTime.UtcNow,
            CameraName: "TestCamera",
            ImageBytes: await File.ReadAllBytesAsync("bus.jpg"),
            ImageName: "bus.jpg",
            ImageCreatedDateTime: DateTime.UtcNow
        );
        var result = await _sut.AnalyseImage(imageRecordedEvent, default);
        Assert.That(result[0].DetectionData, Is.EqualTo("4 persons"));
    }
}