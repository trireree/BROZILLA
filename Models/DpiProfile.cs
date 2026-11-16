namespace BROZILLA.Models;

/// <summary>
/// Represents a GoodbyeDPI preset profile for Turkish ISPs.
/// </summary>
public sealed record DpiProfile(
    string Id,
    string Name,
    string Subtitle,
    string Arguments)
{
    public static IReadOnlyList<DpiProfile> All { get; } =
    [
        new(
            "turkey_standard",
            "Standart (Türkiye Genel)",
            "Türk Telekom, TurkNet, Kablonet",
            "-5 --set-ttl 5 --dns-addr 77.88.8.8 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:0ff --dnsv6-port 1253"
        ),
        new(
            "superonline_alt1",
            "Superonline / Vodafone — Alt 1",
            "TTL 3 (DNS yönlendirmesiz)",
            "--set-ttl 3"
        ),
        new(
            "superonline_alt2",
            "Superonline / Vodafone — Alt 2",
            "Mod -5 (DNS yönlendirmesiz)",
            "-5"
        ),
        new(
            "superonline_alt3",
            "Superonline / Vodafone — Alt 3",
            "TTL 3 + DNS Yönlendirme",
            "--set-ttl 3 --dns-addr 77.88.8.8 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:0ff --dnsv6-port 1253"
        ),
        new(
            "superonline_alt4",
            "Superonline / Vodafone — Alt 4",
            "Mod -5 + DNS Yönlendirme",
            "-5 --dns-addr 77.88.8.8 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:0ff --dnsv6-port 1253"
        ),
        new(
            "superonline_alt5",
            "Superonline / Vodafone — Alt 5",
            "Mod -9 + DNS Yönlendirme",
            "-9 --dns-addr 77.88.8.8 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:0ff --dnsv6-port 1253"
        ),
        new(
            "superonline_alt6",
            "Superonline / Vodafone — Alt 6",
            "Mod -9 (DNS yönlendirmesiz)",
            "-9"
        )
    ];

    public static DpiProfile GetById(string? id) =>
        All.FirstOrDefault(p => p.Id == id) ?? All[0];
}
