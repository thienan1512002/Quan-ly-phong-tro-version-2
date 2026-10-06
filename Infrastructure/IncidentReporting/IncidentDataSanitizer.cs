using System.Data.Common;
using System.Text.RegularExpressions;

namespace QuanLyPhongTro.Infrastructure.IncidentReporting;

public sealed class IncidentDataSanitizer
{
    private const string Redacted = "[REDACTED]";
    private static readonly Regex SensitiveAssignment = CreateRegex(
        @"([""']?(?:password|pwd|(?:access[_-]?|refresh[_-]?|id[_-]?|bot[_-]?)?token|client[_-]?secret|api[_-]?key|authorization|cookie|connection\s*string|connectionstring|server|data\s*source|database|initial\s*catalog|user\s*id|uid)[""']?\s*[:=]\s*)(?:""(?:\\.|[^""])*""|'(?:\\.|[^'])*'|[^\s,;]+)");
    private static readonly Regex Authentication = CreateRegex(@"\b(?:Bearer|Basic)\s+[a-z0-9._~+/-]+=*");
    private static readonly Regex Jwt = CreateRegex(@"\beyJ[a-z0-9_-]*\.[a-z0-9_-]+\.[a-z0-9_-]+\b");
    private static readonly Regex UriCredentials = CreateRegex(@"(?<=://)[^/\s@]+@");
    private static readonly Regex Email = CreateRegex(@"\b[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}\b");
    private static readonly Regex Phone = CreateRegex(@"(?<!\w)(?:\+?84|0)[\s.-]?(?:\d[\s.-]?){8,10}(?!\w)");
    private readonly string[] _configuredSecrets;

    public IncidentDataSanitizer(IConfiguration configuration, ILogger<IncidentDataSanitizer> logger)
    {
        var secrets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var setting in configuration.AsEnumerable())
        {
            if (string.IsNullOrEmpty(setting.Value))
                continue;

            if (setting.Key.StartsWith("ConnectionStrings:", StringComparison.OrdinalIgnoreCase))
            {
                secrets.Add(setting.Value);
                try
                {
                    var builder = new DbConnectionStringBuilder { ConnectionString = setting.Value };
                    foreach (string key in builder.Keys)
                        if (IsSensitiveKey(key))
                            secrets.Add(Convert.ToString(builder[key]) ?? string.Empty);
                }
                catch (ArgumentException)
                {
                    logger.LogWarning("A connection string could not be parsed for incident redaction; assignment redaction remains enabled.");
                }
            }
            else if (setting.Key.Split(':').Any(IsSensitiveKey))
            {
                secrets.Add(setting.Value);
            }
        }

        _configuredSecrets = secrets.Where(value => value.Length > 0)
            .OrderByDescending(value => value.Length).ToArray();
    }

    public string? Sanitize(string? value, int maxLength, HttpContext context)
    {
        if (value is null)
            return null;

        // Request secrets are used only for redaction, never included in the payload.
        // Do not read Request.Body or Request.Form.
        var secrets = _configuredSecrets.Concat(GetRequestSecrets(context))
            .Where(secret => !string.IsNullOrEmpty(secret)).Distinct(StringComparer.Ordinal)
            .OrderByDescending(secret => secret.Length);
        foreach (var secret in secrets)
        {
            value = value.Replace(secret, Redacted, StringComparison.Ordinal);
            value = value.Replace(Uri.EscapeDataString(secret), Redacted, StringComparison.OrdinalIgnoreCase);
        }

        value = Authentication.Replace(value, Redacted);
        value = Jwt.Replace(value, Redacted);
        value = SensitiveAssignment.Replace(value, "$1" + Redacted);
        value = UriCredentials.Replace(value, Redacted + "@");
        value = Email.Replace(value, Redacted);
        value = Phone.Replace(value, Redacted);
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static IEnumerable<string> GetRequestSecrets(HttpContext context)
    {
        foreach (var authorization in context.Request.Headers.Authorization)
        {
            if (string.IsNullOrEmpty(authorization))
                continue;
            yield return authorization;
            var separator = authorization.IndexOf(' ');
            if (separator >= 0)
                yield return authorization[(separator + 1)..].Trim();
        }

        foreach (var cookie in context.Request.Headers.Cookie)
            if (!string.IsNullOrEmpty(cookie))
                yield return cookie;
        foreach (var cookie in context.Request.Cookies)
            yield return cookie.Value;
        foreach (var parameter in context.Request.Query.Where(parameter => IsSensitiveKey(parameter.Key)))
            foreach (var item in parameter.Value)
                if (!string.IsNullOrEmpty(item))
                    yield return item;
    }

    private static bool IsSensitiveKey(string key)
    {
        var normalized = key.Replace("_", "").Replace("-", "").Replace(" ", "").ToLowerInvariant();
        return normalized is "password" or "pwd" or "secret" or "clientsecret" or "apikey" or
            "authorization" or "cookie" or "connectionstring" or "userid" or "uid" ||
            normalized.EndsWith("token", StringComparison.Ordinal);
    }

    private static Regex CreateRegex(string pattern) => new(pattern,
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(50));
}
