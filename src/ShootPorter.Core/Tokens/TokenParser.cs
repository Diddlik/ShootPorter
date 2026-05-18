namespace ShootPorter.Core.Tokens;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// Parses template strings containing {Token} placeholders and substitutes resolved values.
/// Supports format specifiers via colon syntax (e.g., {Seq:0000}) and string functions.
/// </summary>
public sealed partial class TokenParser
{
    private readonly TokenRegistry _registry;

    public TokenParser(TokenRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    /// <summary>
    /// Replaces all {Token} placeholders in the template with resolved values.
    /// Unknown tokens are left as-is. Supports {Seq:0000} format syntax.
    /// </summary>
    public string Parse(string template, TokenContext context)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(context);

        return TokenPattern().Replace(template, match =>
        {
            var raw = match.Groups[1].Value;

            // Handle string functions first
            if (TryResolveStringFunction(raw, context, out var functionResult))
                return functionResult;

            // Handle file[...] syntax
            if (TryResolveFileSlice(raw, context, out var fileResult))
                return fileResult;

            var colonIndex = raw.IndexOf(':');

            string tokenName;
            string? format;

            if (colonIndex >= 0)
            {
                tokenName = raw[..colonIndex];
                format = raw[(colonIndex + 1)..];
            }
            else
            {
                tokenName = raw;
                format = null;
            }

            var resolved = _registry.Resolve(tokenName, context);
            if (resolved is null)
                return match.Value; // leave unknown tokens as-is

            // Apply format if specified and value is numeric
            if (format is not null && int.TryParse(resolved, out var numericValue))
                return numericValue.ToString(format);

            return resolved;
        });
    }

    private bool TryResolveStringFunction(string raw, TokenContext context, out string result)
    {
        result = string.Empty;

        // {left,n,str}
        var leftMatch = LeftFunctionRegex().Match(raw);
        if (leftMatch.Success)
        {
            var n = int.Parse(leftMatch.Groups[1].Value);
            var str = ResolveNestedToken(leftMatch.Groups[2].Value, context);
            result = str.Length >= n ? str[..n] : str;
            return true;
        }

        // {mid,n,m,str}
        var midMatch = MidFunctionRegex().Match(raw);
        if (midMatch.Success)
        {
            var n = int.Parse(midMatch.Groups[1].Value);
            var m = int.Parse(midMatch.Groups[2].Value);
            var str = ResolveNestedToken(midMatch.Groups[3].Value, context);
            if (n < str.Length)
            {
                var len = Math.Min(m, str.Length - n);
                result = str.Substring(n, len);
            }
            return true;
        }

        // {right,n,str}
        var rightMatch = RightFunctionRegex().Match(raw);
        if (rightMatch.Success)
        {
            var n = int.Parse(rightMatch.Groups[1].Value);
            var str = ResolveNestedToken(rightMatch.Groups[2].Value, context);
            result = str.Length >= n ? str[^n..] : str;
            return true;
        }

        // {first,str}
        var firstMatch = FirstFunctionRegex().Match(raw);
        if (firstMatch.Success)
        {
            var str = ResolveNestedToken(firstMatch.Groups[1].Value, context);
            var spaceIndex = str.IndexOf(' ');
            result = spaceIndex >= 0 ? str[..spaceIndex] : str;
            return true;
        }

        // {last,str}
        var lastMatch = LastFunctionRegex().Match(raw);
        if (lastMatch.Success)
        {
            var str = ResolveNestedToken(lastMatch.Groups[1].Value, context);
            var spaceIndex = str.LastIndexOf(' ');
            result = spaceIndex >= 0 ? str[(spaceIndex + 1)..] : str;
            return true;
        }

        // {upper,str}
        var upperMatch = UpperFunctionRegex().Match(raw);
        if (upperMatch.Success)
        {
            var str = ResolveNestedToken(upperMatch.Groups[1].Value, context);
            result = str.ToUpperInvariant();
            return true;
        }

        // {lower,str}
        var lowerMatch = LowerFunctionRegex().Match(raw);
        if (lowerMatch.Success)
        {
            var str = ResolveNestedToken(lowerMatch.Groups[1].Value, context);
            result = str.ToLowerInvariant();
            return true;
        }

        // {capitalize,str}
        var capitalizeMatch = CapitalizeFunctionRegex().Match(raw);
        if (capitalizeMatch.Success)
        {
            var str = ResolveNestedToken(capitalizeMatch.Groups[1].Value, context);
            result = str.Length > 0 
                ? char.ToUpper(str[0], CultureInfo.CurrentCulture) + str[1..].ToLowerInvariant() 
                : str;
            return true;
        }

        // {default,str1,str2}
        var defaultMatch = DefaultFunctionRegex().Match(raw);
        if (defaultMatch.Success)
        {
            var str1 = ResolveNestedToken(defaultMatch.Groups[1].Value, context);
            var str2 = ResolveNestedToken(defaultMatch.Groups[2].Value, context);
            result = string.IsNullOrEmpty(str1) ? str2 : str1;
            return true;
        }

        // {if,teststr,str1,str2}
        var ifMatch = IfFunctionRegex().Match(raw);
        if (ifMatch.Success)
        {
            var testStr = ResolveNestedToken(ifMatch.Groups[1].Value, context);
            var str1 = ResolveNestedToken(ifMatch.Groups[2].Value, context);
            var str2 = ResolveNestedToken(ifMatch.Groups[3].Value, context);
            result = string.IsNullOrEmpty(testStr) ? str1 : str2;
            return true;
        }

        // {cc1,code} - country code to name
        var cc1Match = CountryCode1Regex().Match(raw);
        if (cc1Match.Success)
        {
            var code = cc1Match.Groups[1].Value.ToUpperInvariant();
            result = GetCountryName(code);
            return true;
        }

        // {cc2,code} - 3-char to 2-char country code
        var cc2Match = CountryCode2Regex().Match(raw);
        if (cc2Match.Success)
        {
            var code = cc2Match.Groups[1].Value.ToUpperInvariant();
            result = ConvertCountryCode3To2(code);
            return true;
        }

        return false;
    }

    private bool TryResolveFileSlice(string raw, TokenContext context, out string result)
    {
        result = string.Empty;

        // {file[n-m]} - chars n to m
        var rangeMatch = FileRangeRegex().Match(raw);
        if (rangeMatch.Success)
        {
            var n = int.Parse(rangeMatch.Groups[1].Value);
            var m = int.Parse(rangeMatch.Groups[2].Value);
            var filename = context.OriginalFileName;
            if (n < filename.Length && m >= n)
            {
                var endIndex = Math.Min(m + 1, filename.Length);
                result = filename[n..endIndex];
            }
            return true;
        }

        // {file[n-]} - from char n to end
        var fromMatch = FileFromRegex().Match(raw);
        if (fromMatch.Success)
        {
            var n = int.Parse(fromMatch.Groups[1].Value);
            var filename = context.OriginalFileName;
            if (n < filename.Length)
            {
                result = filename[n..];
            }
            return true;
        }

        // {file[n]} - single char
        var singleMatch = FileSingleRegex().Match(raw);
        if (singleMatch.Success)
        {
            var n = int.Parse(singleMatch.Groups[1].Value);
            var filename = context.OriginalFileName;
            if (n < filename.Length)
            {
                result = filename[n].ToString();
            }
            return true;
        }

        return false;
    }

    private string ResolveNestedToken(string value, TokenContext context)
    {
        // If value looks like a token, resolve it
        var resolved = _registry.Resolve(value, context);
        return resolved ?? value;
    }

    private static string GetCountryName(string code)
    {
        try
        {
            var region = new RegionInfo(code);
            return region.EnglishName;
        }
        catch
        {
            return code;
        }
    }

    private static string ConvertCountryCode3To2(string code3)
    {
        // Common 3-letter to 2-letter mappings
        return code3 switch
        {
            "USA" => "US",
            "GBR" => "GB",
            "DEU" => "DE",
            "FRA" => "FR",
            "ESP" => "ES",
            "ITA" => "IT",
            "JPN" => "JP",
            "CHN" => "CN",
            "AUS" => "AU",
            "CAN" => "CA",
            "BRA" => "BR",
            "MEX" => "MX",
            "NLD" => "NL",
            "BEL" => "BE",
            "AUT" => "AT",
            "CHE" => "CH",
            _ => code3.Length >= 2 ? code3[..2] : code3
        };
    }

    /// <summary>
    /// Appends a uniqueness suffix (_01, _02, etc.) to a file path until the existsCheck returns false.
    /// </summary>
    public static string MakeUnique(string basePath, Func<string, bool> existsCheck)
    {
        ArgumentNullException.ThrowIfNull(basePath);
        ArgumentNullException.ThrowIfNull(existsCheck);

        if (!existsCheck(basePath))
            return basePath;

        var directory = Path.GetDirectoryName(basePath) ?? string.Empty;
        var nameWithoutExt = Path.GetFileNameWithoutExtension(basePath);
        var extension = Path.GetExtension(basePath);

        for (var i = 1; i <= 9999; i++)
        {
            var candidate = Path.Combine(directory, $"{nameWithoutExt}_{i:D2}{extension}");
            if (!existsCheck(candidate))
                return candidate;
        }

        throw new InvalidOperationException($"Could not generate a unique path for '{basePath}' after 9999 attempts.");
    }

    [GeneratedRegex(@"\{([^}]+)\}")]
    private static partial Regex TokenPattern();

    // String function regexes
    [GeneratedRegex(@"^left,(\d+),(.+)$")]
    private static partial Regex LeftFunctionRegex();

    [GeneratedRegex(@"^mid,(\d+),(\d+),(.+)$")]
    private static partial Regex MidFunctionRegex();

    [GeneratedRegex(@"^right,(\d+),(.+)$")]
    private static partial Regex RightFunctionRegex();

    [GeneratedRegex(@"^first,(.+)$")]
    private static partial Regex FirstFunctionRegex();

    [GeneratedRegex(@"^last,(.+)$")]
    private static partial Regex LastFunctionRegex();

    [GeneratedRegex(@"^upper,(.+)$")]
    private static partial Regex UpperFunctionRegex();

    [GeneratedRegex(@"^lower,(.+)$")]
    private static partial Regex LowerFunctionRegex();

    [GeneratedRegex(@"^capitalize,(.+)$")]
    private static partial Regex CapitalizeFunctionRegex();

    [GeneratedRegex(@"^default,([^,]*),(.+)$")]
    private static partial Regex DefaultFunctionRegex();

    [GeneratedRegex(@"^if,([^,]*),([^,]*),(.+)$")]
    private static partial Regex IfFunctionRegex();

    [GeneratedRegex(@"^cc1,(\w+)$")]
    private static partial Regex CountryCode1Regex();

    [GeneratedRegex(@"^cc2,(\w+)$")]
    private static partial Regex CountryCode2Regex();

    // File slice regexes
    [GeneratedRegex(@"^file\[(\d+)-(\d+)\]$")]
    private static partial Regex FileRangeRegex();

    [GeneratedRegex(@"^file\[(\d+)-\]$")]
    private static partial Regex FileFromRegex();

    [GeneratedRegex(@"^file\[(\d+)\]$")]
    private static partial Regex FileSingleRegex();
}
