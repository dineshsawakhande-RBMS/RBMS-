using System.Text.Json;
using System.Text.Json.Serialization;

namespace RBMS.IntegrationTests;

/// <summary>
/// JSON options matching the API (web defaults + string enums), for deserializing
/// responses that contain enum-typed properties in tests.
/// </summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
