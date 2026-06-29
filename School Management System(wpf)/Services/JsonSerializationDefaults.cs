using System.Text.Json;
using System.Text.Json.Serialization;

namespace School_Management_System.Services
{
    internal static class JsonSerializationDefaults
    {
        internal static readonly JsonSerializerOptions SafeGraph = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}
