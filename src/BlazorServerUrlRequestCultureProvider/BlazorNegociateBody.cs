using JetBrains.Annotations;
using System.Text.Json.Serialization;

namespace BlazorServerUrlRequestCultureProvider
{
    [UsedImplicitly]
    public class BlazorNegociateBody
    {
        [UsedImplicitly]
        [JsonPropertyName("negotiateVersion")]
        public int NegotiateVersion { get; set; }

        [UsedImplicitly]
        [JsonPropertyName("connectionToken")]
        public string ConnectionToken { get; set; } = string.Empty;
    }
}
