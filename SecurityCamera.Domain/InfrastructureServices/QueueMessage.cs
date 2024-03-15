using System.Text.Json;
using System.Text.Json.Serialization;
using SecurityCamera.SharedKernel;

namespace SecurityCamera.Domain.InfrastructureServices;

public class QueueMessage
{
    public required string QueueName { get; set; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
    public byte[] ToByteArray()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this);
    }

    public static T? FromByteArray<T>(byte[] byteArray)
    {
        return JsonSerializer.Deserialize<T>(byteArray);
    }
}

