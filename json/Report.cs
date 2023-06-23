using ReleaseJson;

namespace Report;

public record ReleaseReport(string ReportDate, IList<Version> Versions);

public record Version(string MajorVersion, bool Supported, string EolDate, int SupportEndsInDays, IList<Release> Releases);

public record Release(string BuildVersion, bool Security, string ReleaseDate, int ReleasedDaysAgo, IList<Cve> Cves);