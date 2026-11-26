using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HudhudUnlocker;

/// <summary>Gumroad lisans anahtarını Gumroad API'siyle doğrular.</summary>
public static class LicenseVerifier
{
    public enum Result { Valid, Invalid, NetworkError }

    public static async Task<Result> VerifyAsync(string productId, string key)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var form = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                new("product_id", productId),
                new("license_key", key.Trim()),
                new("increment_uses_count", "false")
            });
            var resp = await http.PostAsync("https://api.gumroad.com/v2/licenses/verify", form);
            if ((int)resp.StatusCode >= 500) return Result.NetworkError;
            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (!root.TryGetProperty("success", out var ok) || ok.ValueKind != JsonValueKind.True) return Result.Invalid;
            if (root.TryGetProperty("purchase", out var p))
                foreach (var f in new[] { "refunded", "chargebacked", "disputed" })
                    if (p.TryGetProperty(f, out var v) && v.ValueKind == JsonValueKind.True) return Result.Invalid;
            return Result.Valid;
        }
        catch { return Result.NetworkError; }
    }
}
