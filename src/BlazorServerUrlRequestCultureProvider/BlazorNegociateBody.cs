using System.Text.Json.Serialization;
using JetBrains.Annotations;

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