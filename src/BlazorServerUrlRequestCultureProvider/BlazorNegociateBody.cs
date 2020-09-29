using System.Text.Json.Serialization;

namespace BlazorServerUrlRequestCultureProvider
{
    public class BlazorNegociateBody
    {
        [JsonPropertyName("negotiateVersion")]
        public int NegotiateVersion { get; set; }

        [JsonPropertyName("connectionToken")]
        public string ConnectionToken { get; set; } = string.Empty;
    }
}
