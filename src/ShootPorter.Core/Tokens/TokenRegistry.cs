using System.Globalization;
using System.Text.RegularExpressions;

namespace ShootPorter.Core.Tokens;

/// <summary>
/// Maintains the canonical mapping of token names to their definitions and resolver functions.
/// All built-in tokens are registered in the constructor; custom tokens may be added via
/// <see cref="Register"/>.
/// </summary>
public sealed partial class TokenRegistry
{
    private readonly Dictionary<string, (TokenDefinition Definition, Func<TokenContext, string> Resolver)> _tokens = new(StringComparer.Ordinal);

    /// <summary>Initialises the registry and registers all built-in tokens.</summary>
    public TokenRegistry()
    {
        RegisterDateTimeTokens();
        RegisterDateTimeNowTokens();
        RegisterCameraTokens();
        RegisterFileTokens();
        RegisterJobSequenceTokens();
    }

    private void RegisterDateTimeTokens()
    {
        // Date tokens
        Register(new TokenDefinition("d", "Date YYMMDD", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("yyMMdd"));
        Register(new TokenDefinition("t", "Time HHMMSS", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("HHmmss"));
        Register(new TokenDefinition("x", "Date for locale", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("d", ctx.Culture));
        Register(new TokenDefinition("X", "Time for locale (underscores)", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("T", ctx.Culture).Replace(':', '_'));
        Register(new TokenDefinition("y", "Year without century", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("yy"));
        Register(new TokenDefinition("Y", "Year with century", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("yyyy"));
        Register(new TokenDefinition("m", "Month (01-12)", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("MM"));
        Register(new TokenDefinition("b", "Abbreviated month name", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("MMM", ctx.Culture));
        Register(new TokenDefinition("B", "Full month name", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("MMMM", ctx.Culture));
        Register(new TokenDefinition("D", "Day of month (01-31)", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("dd"));
        Register(new TokenDefinition("j", "Day of year (001-366)", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.DayOfYear.ToString("D3"));
        Register(new TokenDefinition("W", "Week number (00-53)", TokenCategory.DateTime),
            ctx => GetWeekNumber(ctx.CaptureDateTime, ctx.Culture).ToString("D2"));
        Register(new TokenDefinition("WI", "ISO week number", TokenCategory.DateTime),
            ctx => ISOWeek.GetWeekOfYear(ctx.CaptureDateTime.DateTime).ToString("D2"));
        Register(new TokenDefinition("IWD", "Full ISO week date", TokenCategory.DateTime),
            ctx => $"{ISOWeek.GetYear(ctx.CaptureDateTime.DateTime)}-W{ISOWeek.GetWeekOfYear(ctx.CaptureDateTime.DateTime):D2}-{(int)ctx.CaptureDateTime.DayOfWeek switch { 0 => 7, var d => d }}");
        Register(new TokenDefinition("a", "Abbreviated weekday", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("ddd", ctx.Culture));
        Register(new TokenDefinition("A", "Full weekday name", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("dddd", ctx.Culture));
        Register(new TokenDefinition("P", "Quarter (1-4)", TokenCategory.DateTime),
            ctx => ((ctx.CaptureDateTime.Month - 1) / 3 + 1).ToString());
        Register(new TokenDefinition("H", "Hour (00-23)", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("HH"));
        Register(new TokenDefinition("I", "Hour (01-12)", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("hh"));
        Register(new TokenDefinition("M", "Minutes (00-59)", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("mm"));
        Register(new TokenDefinition("S", "Seconds (00-59)", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("ss"));
        Register(new TokenDefinition("s", "Subseconds", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("ff"));
        Register(new TokenDefinition("p", "AM/PM (upper)", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("tt", CultureInfo.InvariantCulture).ToUpperInvariant());
        Register(new TokenDefinition("plc", "am/pm (lower)", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("tt", CultureInfo.InvariantCulture).ToLowerInvariant());
        Register(new TokenDefinition("z", "Time zone name", TokenCategory.DateTime),
            ctx => TimeZoneInfo.Local.DisplayName);
        Register(new TokenDefinition("Z", "Time zone offset", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("zzz").Replace(":", ""));
        Register(new TokenDefinition("#c", "Long date/time for locale", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("F", ctx.Culture));
        Register(new TokenDefinition("#x", "Long date for locale", TokenCategory.DateTime),
            ctx => ctx.CaptureDateTime.ToString("D", ctx.Culture));
    }

    private void RegisterDateTimeNowTokens()
    {
        var now = DateTimeOffset.Now;
        Register(new TokenDefinition("1", "Year 'now' with century", TokenCategory.DateTimeNow),
            _ => DateTimeOffset.Now.ToString("yyyy"));
        Register(new TokenDefinition("2", "Month 'now' (01-12)", TokenCategory.DateTimeNow),
            _ => DateTimeOffset.Now.ToString("MM"));
        Register(new TokenDefinition("3", "Day 'now' (01-31)", TokenCategory.DateTimeNow),
            _ => DateTimeOffset.Now.ToString("dd"));
        Register(new TokenDefinition("4", "Year 'now' without century", TokenCategory.DateTimeNow),
            _ => DateTimeOffset.Now.ToString("yy"));
        Register(new TokenDefinition("5", "Year less 3 hours", TokenCategory.DateTimeNow),
            _ => DateTimeOffset.Now.AddHours(-3).ToString("yyyy"));
        Register(new TokenDefinition("6", "Month less 3 hours", TokenCategory.DateTimeNow),
            _ => DateTimeOffset.Now.AddHours(-3).ToString("MM"));
        Register(new TokenDefinition("7", "Day less 3 hours", TokenCategory.DateTimeNow),
            _ => DateTimeOffset.Now.AddHours(-3).ToString("dd"));
    }

    private void RegisterCameraTokens()
    {
        Register(new TokenDefinition("c", "Camera serial number", TokenCategory.Camera),
            ctx => ctx.CameraSerialNumber ?? string.Empty);
        Register(new TokenDefinition("C", "Canon EOS-1D serial", TokenCategory.Camera),
            ctx => ctx.CameraSerialNumber ?? string.Empty);
        Register(new TokenDefinition("i", "ISO", TokenCategory.Camera),
            ctx => ctx.IsoSpeed?.ToString() ?? string.Empty);
        Register(new TokenDefinition("k", "ISO (Auto=0)", TokenCategory.Camera),
            ctx => ctx.IsoSpeed == 0 ? "Auto" : ctx.IsoSpeed?.ToString() ?? string.Empty);
        Register(new TokenDefinition("K1", "Focal length mm", TokenCategory.Camera),
            ctx => ctx.FocalLength ?? string.Empty);
        Register(new TokenDefinition("K2", "Aperture", TokenCategory.Camera),
            ctx => ctx.Aperture ?? string.Empty);
        Register(new TokenDefinition("K3", "Shutter speed denominator", TokenCategory.Camera),
            ctx => ExtractShutterDenominator(ctx.ShutterSpeed));
        Register(new TokenDefinition("O", "Owner string", TokenCategory.Camera),
            ctx => ctx.Owner ?? string.Empty);
        Register(new TokenDefinition("copyright", "Copyright", TokenCategory.Camera),
            ctx => ctx.Copyright ?? string.Empty);
        Register(new TokenDefinition("T", "Camera model (first digit word)", TokenCategory.Camera),
            ctx => ExtractFirstDigitWord(ctx.CameraModel, false));
        Register(new TokenDefinition("T1", "Camera model (first digit word, hyphen=space)", TokenCategory.Camera),
            ctx => ExtractFirstDigitWord(ctx.CameraModel, true));
        Register(new TokenDefinition("T2", "Full camera name", TokenCategory.Camera),
            ctx => $"{ctx.CameraManufacturer} {ctx.CameraModel}".Trim());
        Register(new TokenDefinition("T3", "First word with digits", TokenCategory.Camera),
            ctx => ExtractFirstDigitWord(ctx.CameraModel, false));
        Register(new TokenDefinition("T4", "First word with digits (hyphen=space)", TokenCategory.Camera),
            ctx => ExtractFirstDigitWord(ctx.CameraModel, true));
        Register(new TokenDefinition("T5", "Last word with digits", TokenCategory.Camera),
            ctx => ExtractLastDigitWord(ctx.CameraModel, false));
        Register(new TokenDefinition("T6", "Last word with digits (hyphen=space)", TokenCategory.Camera),
            ctx => ExtractLastDigitWord(ctx.CameraModel, true));
        Register(new TokenDefinition("T8", "Camera mapping value", TokenCategory.Camera),
            ctx => ctx.CameraMappings.TryGetValue("T8", out var v) ? v : ExtractFirstDigitWord(ctx.CameraModel, true));
        Register(new TokenDefinition("T9", "Camera mapping value", TokenCategory.Camera),
            ctx => ctx.CameraMappings.TryGetValue("T9", out var v) ? v : ExtractFirstDigitWord(ctx.CameraModel, true));
        Register(new TokenDefinition("r8", "Image counter", TokenCategory.Camera),
            ctx => ctx.ImageCounter?.ToString() ?? "0");
        Register(new TokenDefinition("r85", "Image counter (5 digits)", TokenCategory.Camera),
            ctx => (ctx.ImageCounter ?? 0).ToString("D5"));
        Register(new TokenDefinition("r86", "Image counter (6 digits)", TokenCategory.Camera),
            ctx => (ctx.ImageCounter ?? 0).ToString("D6"));
    }

    private void RegisterFileTokens()
    {
        Register(new TokenDefinition("e", "Extension without dot", TokenCategory.File),
            ctx => ctx.Extension.TrimStart('.'));
        Register(new TokenDefinition("E", "File type (JPG/RAW)", TokenCategory.File),
            ctx => IsJpeg(ctx.Extension) ? "JPG" : "RAW");
        Register(new TokenDefinition("E1", "File type (empty for JPG)", TokenCategory.File),
            ctx => IsJpeg(ctx.Extension) ? string.Empty : "RAW");
        Register(new TokenDefinition("E2", "File type (JPG or empty)", TokenCategory.File),
            ctx => IsJpeg(ctx.Extension) ? "JPG" : string.Empty);
        Register(new TokenDefinition("f", "First 3 chars of filename", TokenCategory.File),
            ctx => ctx.OriginalFileName.Length >= 3 ? ctx.OriginalFileName[..3] : ctx.OriginalFileName);
        Register(new TokenDefinition("F", "Filename without extension", TokenCategory.File),
            ctx => ctx.OriginalFileName);
        Register(new TokenDefinition("o", "Image folder name", TokenCategory.File),
            ctx => ctx.SourceFolderName);
        Register(new TokenDefinition("q", "Image folder number", TokenCategory.File),
            ctx => ctx.SourceFolderNumber);
        Register(new TokenDefinition("r", "Image number", TokenCategory.File),
            ctx => ctx.ImageNumber);
        Register(new TokenDefinition("r1", "Last 1 digit of image number", TokenCategory.File),
            ctx => GetLastNDigits(ctx.ImageNumber, 1));
        Register(new TokenDefinition("r2", "Last 2 digits of image number", TokenCategory.File),
            ctx => GetLastNDigits(ctx.ImageNumber, 2));
        Register(new TokenDefinition("r3", "Last 3 digits of image number", TokenCategory.File),
            ctx => GetLastNDigits(ctx.ImageNumber, 3));
        Register(new TokenDefinition("r4", "Last 4 digits of image number", TokenCategory.File),
            ctx => GetLastNDigits(ctx.ImageNumber, 4));
        Register(new TokenDefinition("u", "My Pictures folder", TokenCategory.File),
            _ => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
    }

    private void RegisterJobSequenceTokens()
    {
        Register(new TokenDefinition("J", "Job code", TokenCategory.Job),
            ctx => ctx.JobCode ?? string.Empty);
        Register(new TokenDefinition("Jc_fw", "First word of job code", TokenCategory.Job),
            ctx => GetFirstWord(ctx.JobCode));
        Register(new TokenDefinition("l", "Uniqueness number (empty if unique)", TokenCategory.Job),
            ctx => ctx.UniquenessNumber == 0 ? string.Empty : ctx.UniquenessNumber.ToString());
        Register(new TokenDefinition("l2", "Uniqueness number (2 digits)", TokenCategory.Job),
            ctx => ctx.UniquenessNumber == 0 ? string.Empty : ctx.UniquenessNumber.ToString("D2"));
        Register(new TokenDefinition("l3", "Uniqueness number (3 digits)", TokenCategory.Job),
            ctx => ctx.UniquenessNumber == 0 ? string.Empty : ctx.UniquenessNumber.ToString("D3"));
        Register(new TokenDefinition("l4", "Uniqueness number (4 digits)", TokenCategory.Job),
            ctx => ctx.UniquenessNumber == 0 ? string.Empty : ctx.UniquenessNumber.ToString("D4"));
        Register(new TokenDefinition("L", "Uniqueness number (always)", TokenCategory.Job),
            ctx => (ctx.UniquenessNumber == 0 ? 1 : ctx.UniquenessNumber).ToString());
        Register(new TokenDefinition("seq#", "Download sequence number", TokenCategory.Sequence),
            ctx => ctx.SequenceNumber.ToString());
        Register(new TokenDefinition("seq#2", "Sequence (2 digits)", TokenCategory.Sequence),
            ctx => ctx.SequenceNumber.ToString("D2"));
        Register(new TokenDefinition("seq#3", "Sequence (3 digits)", TokenCategory.Sequence),
            ctx => ctx.SequenceNumber.ToString("D3"));
        Register(new TokenDefinition("seq#4", "Sequence (4 digits)", TokenCategory.Sequence),
            ctx => ctx.SequenceNumber.ToString("D4"));
        Register(new TokenDefinition("seq#5", "Sequence (5 digits)", TokenCategory.Sequence),
            ctx => ctx.SequenceNumber.ToString("D5"));
        Register(new TokenDefinition("seq#6", "Sequence (6 digits)", TokenCategory.Sequence),
            ctx => ctx.SequenceNumber.ToString("D6"));
        Register(new TokenDefinition("n1", "Session sequence (1 digit)", TokenCategory.Sequence),
            ctx => ctx.SessionSequenceNumber.ToString());
        Register(new TokenDefinition("n2", "Session sequence (2 digits)", TokenCategory.Sequence),
            ctx => ctx.SessionSequenceNumber.ToString("D2"));
        Register(new TokenDefinition("n3", "Session sequence (3 digits)", TokenCategory.Sequence),
            ctx => ctx.SessionSequenceNumber.ToString("D3"));
        Register(new TokenDefinition("n4", "Session sequence (4 digits)", TokenCategory.Sequence),
            ctx => ctx.SessionSequenceNumber.ToString("D4"));
        Register(new TokenDefinition("R", "Downloads today", TokenCategory.Sequence),
            ctx => ctx.DailyDownloadCount.ToString());
    }

    /// <summary>Registers a token with the given definition and resolver.</summary>
    public void Register(TokenDefinition definition, Func<TokenContext, string> resolver)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(resolver);

        _tokens[definition.Name] = (definition, resolver);
    }

    /// <summary>
    /// Attempts to resolve a token by name. Returns <see langword="null"/> if the token is not registered.
    /// </summary>
    public string? Resolve(string tokenName, TokenContext context)
    {
        ArgumentNullException.ThrowIfNull(tokenName);
        ArgumentNullException.ThrowIfNull(context);

        return _tokens.TryGetValue(tokenName, out var entry) ? entry.Resolver(context) : null;
    }

    /// <summary>Returns <see langword="true"/> when a token with the given name is registered.</summary>
    public bool IsRegistered(string tokenName)
    {
        ArgumentNullException.ThrowIfNull(tokenName);
        return _tokens.ContainsKey(tokenName);
    }

    /// <summary>Returns definitions for all registered tokens, suitable for populating a UI picker.</summary>
    public IReadOnlyList<TokenDefinition> GetAllDefinitions() =>
        _tokens.Values.Select(e => e.Definition).ToList();

    // Helper methods
    private static int GetWeekNumber(DateTimeOffset date, CultureInfo culture)
    {
        var cal = culture.Calendar;
        var rule = culture.DateTimeFormat.CalendarWeekRule;
        var firstDay = culture.DateTimeFormat.FirstDayOfWeek;
        return cal.GetWeekOfYear(date.DateTime, rule, firstDay);
    }

    private static bool IsJpeg(string extension)
    {
        var ext = extension.TrimStart('.').ToUpperInvariant();
        return ext is "JPG" or "JPEG";
    }

    private static string ExtractFirstDigitWord(string? model, bool treatHyphenAsSpace)
    {
        if (string.IsNullOrEmpty(model)) return string.Empty;
        var text = treatHyphenAsSpace ? model.Replace('-', ' ') : model;
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.FirstOrDefault(w => w.Any(char.IsDigit)) ?? string.Empty;
    }

    private static string ExtractLastDigitWord(string? model, bool treatHyphenAsSpace)
    {
        if (string.IsNullOrEmpty(model)) return string.Empty;
        var text = treatHyphenAsSpace ? model.Replace('-', ' ') : model;
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.LastOrDefault(w => w.Any(char.IsDigit)) ?? string.Empty;
    }

    private static string ExtractShutterDenominator(string? shutterSpeed)
    {
        if (string.IsNullOrEmpty(shutterSpeed)) return string.Empty;
        var match = ShutterDenominatorRegex().Match(shutterSpeed);
        return match.Success ? match.Groups[1].Value : shutterSpeed;
    }

    private static string GetLastNDigits(string imageNumber, int n)
    {
        if (string.IsNullOrEmpty(imageNumber)) return string.Empty;
        return imageNumber.Length >= n ? imageNumber[^n..] : imageNumber.PadLeft(n, '0');
    }

    private static string GetFirstWord(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var spaceIndex = text.IndexOf(' ');
        return spaceIndex >= 0 ? text[..spaceIndex] : text;
    }

    [GeneratedRegex(@"1/(\d+)")]
    private static partial Regex ShutterDenominatorRegex();
}
