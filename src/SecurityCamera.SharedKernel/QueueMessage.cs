using System.Text.Json;

namespace SecurityCamera.SharedKernel;

public class QueueMessage : EventArgs
{
    public required string QueueName { get; set; }

    public static string ToJson<T>(T obj)
    {
        return JsonSerializer.Serialize(obj);
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