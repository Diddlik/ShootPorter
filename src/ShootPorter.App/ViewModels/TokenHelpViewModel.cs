using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// ViewModel for the token help window displaying all available tokens with examples.
/// </summary>
public partial class TokenHelpViewModel : ViewModelBase
{
    public ObservableCollection<TokenHelpCategory> Categories { get; } = [];

    public TokenHelpViewModel()
    {
        LoadTokens();
    }

    private void LoadTokens()
    {
        // Date and Time Tokens
        var dateTimeTokens = new TokenHelpCategory("Date and Time");
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{d}", "Date YYMMDD", "260329"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{t}", "Time HHMMSS", "160922"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{x}", "Date for locale", "29.03.2026"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{X}", "Time for locale (underscores)", "16_09_22"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{y}", "Year without century", "26"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{Y}", "Year with century", "2026"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{m}", "Month (01-12)", "03"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{b}", "Abbreviated month name", "Mar"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{B}", "Full month name", "March"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{D}", "Day of month (01-31)", "29"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{j}", "Day of year (001-366)", "088"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{W}", "Week number (00-53)", "12"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{WI}", "ISO week number", "13"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{IWD}", "Full ISO week date", "2026-W13-7"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{a}", "Abbreviated weekday", "Sun"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{A}", "Full weekday name", "Sunday"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{P}", "Quarter (1-4)", "1"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{H}", "Hour 24h (00-23)", "16"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{I}", "Hour 12h (01-12)", "04"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{M}", "Minutes (00-59)", "09"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{S}", "Seconds (00-59)", "22"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{s}", "Subseconds", "00"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{p}", "AM/PM (upper)", "PM"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{plc}", "am/pm (lower)", "pm"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{z}", "Time zone name", "W. Europe..."));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{Z}", "Time zone offset", "+0200"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{#c}", "Long date/time locale", "Sunday, 29 Mar..."));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{#x}", "Long date locale", "Sunday, 29 Mar..."));
        Categories.Add(dateTimeTokens);

        // Current Time Tokens
        var nowTokens = new TokenHelpCategory("Current Time ('Now')");
        nowTokens.Tokens.Add(new TokenHelpItem("{1}", "Year 'now' with century", "2026"));
        nowTokens.Tokens.Add(new TokenHelpItem("{2}", "Month 'now' (01-12)", "03"));
        nowTokens.Tokens.Add(new TokenHelpItem("{3}", "Day 'now' (01-31)", "29"));
        nowTokens.Tokens.Add(new TokenHelpItem("{4}", "Year 'now' without century", "26"));
        nowTokens.Tokens.Add(new TokenHelpItem("{5}", "Year less 3 hours", "2026"));
        nowTokens.Tokens.Add(new TokenHelpItem("{6}", "Month less 3 hours", "03"));
        nowTokens.Tokens.Add(new TokenHelpItem("{7}", "Day less 3 hours", "29"));
        Categories.Add(nowTokens);

        // Camera/Shooting Data Tokens
        var cameraTokens = new TokenHelpCategory("Camera / Shooting Data");
        cameraTokens.Tokens.Add(new TokenHelpItem("{c}", "Camera serial number", "543467"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{C}", "Canon EOS-1D serial", "AEB"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{i}", "ISO", "100"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{k}", "ISO (0=Auto)", "100"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{K1}", "Focal length mm", "28"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{K2}", "Aperture", "5.6"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{K3}", "Shutter speed denom.", "125"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{O}", "Owner string", "John Smith"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{copyright}", "Copyright", "(C) 2026 John"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T}", "Camera model (digits)", "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T1}", "Model (hyphen=space)", "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T2}", "Full camera name", "Canon EOS 50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T3}", "First word with digits", "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T4}", "T3 (hyphen=space)", "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T5}", "Last word with digits", "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T6}", "T5 (hyphen=space)", "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T8}", "Camera mapping value", "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T9}", "Camera mapping value", "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{r8}", "Image counter", "12345"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{r85}", "Image counter (5 dig)", "12345"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{r86}", "Image counter (6 dig)", "012345"));
        Categories.Add(cameraTokens);

        // File System Tokens
        var fileTokens = new TokenHelpCategory("File System");
        fileTokens.Tokens.Add(new TokenHelpItem("{e}", "Extension without '.'", "JPG"));
        fileTokens.Tokens.Add(new TokenHelpItem("{E}", "File type (JPG/RAW)", "JPG"));
        fileTokens.Tokens.Add(new TokenHelpItem("{E1}", "Type (empty for JPG)", ""));
        fileTokens.Tokens.Add(new TokenHelpItem("{E2}", "Type (JPG or empty)", "JPG"));
        fileTokens.Tokens.Add(new TokenHelpItem("{f}", "First 3 chars of name", "IMG"));
        fileTokens.Tokens.Add(new TokenHelpItem("{F}", "Filename without ext", "IMG_0123"));
        fileTokens.Tokens.Add(new TokenHelpItem("{file[2-5]}", "Chars 2-5 of filename", "G_01"));
        fileTokens.Tokens.Add(new TokenHelpItem("{file[2]}", "Char 2 of filename", "G"));
        fileTokens.Tokens.Add(new TokenHelpItem("{file[4-]}", "From char 4 to end", "0123"));
        fileTokens.Tokens.Add(new TokenHelpItem("{o}", "Image folder name", "100CANON"));
        fileTokens.Tokens.Add(new TokenHelpItem("{q}", "Image folder number", "100"));
        fileTokens.Tokens.Add(new TokenHelpItem("{r}", "Image number", "0123"));
        fileTokens.Tokens.Add(new TokenHelpItem("{r1}", "Last 1 digit", "3"));
        fileTokens.Tokens.Add(new TokenHelpItem("{r2}", "Last 2 digits", "23"));
        fileTokens.Tokens.Add(new TokenHelpItem("{r3}", "Last 3 digits", "123"));
        fileTokens.Tokens.Add(new TokenHelpItem("{r4}", "Last 4 digits", "0123"));
        fileTokens.Tokens.Add(new TokenHelpItem("{u}", "My Pictures folder", "C:\\Users\\..."));
        Categories.Add(fileTokens);

        // Job Code and Numbering
        var jobTokens = new TokenHelpCategory("Job Code and Numbering");
        jobTokens.Tokens.Add(new TokenHelpItem("{J}", "Job code", "Wedding"));
        jobTokens.Tokens.Add(new TokenHelpItem("{Jc_fw}", "First word of job code", "Wedding"));
        jobTokens.Tokens.Add(new TokenHelpItem("{l}", "Uniqueness (empty if unique)", ""));
        jobTokens.Tokens.Add(new TokenHelpItem("{l2}", "Uniqueness (2 digits)", "01"));
        jobTokens.Tokens.Add(new TokenHelpItem("{l3}", "Uniqueness (3 digits)", "001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{l4}", "Uniqueness (4 digits)", "0001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{L}", "Uniqueness (always)", "1"));
        jobTokens.Tokens.Add(new TokenHelpItem("{seq#}", "Download sequence", "1"));
        jobTokens.Tokens.Add(new TokenHelpItem("{seq#2}", "Sequence (2 digits)", "01"));
        jobTokens.Tokens.Add(new TokenHelpItem("{seq#3}", "Sequence (3 digits)", "001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{seq#4}", "Sequence (4 digits)", "0001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{seq#5}", "Sequence (5 digits)", "00001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{seq#6}", "Sequence (6 digits)", "000001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{n1}", "Session seq (1 digit)", "1"));
        jobTokens.Tokens.Add(new TokenHelpItem("{n2}", "Session seq (2 digits)", "01"));
        jobTokens.Tokens.Add(new TokenHelpItem("{n3}", "Session seq (3 digits)", "001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{n4}", "Session seq (4 digits)", "0001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{R}", "Downloads today", "42"));
        Categories.Add(jobTokens);

        // String Functions
        var stringTokens = new TokenHelpCategory("String Functions");
        stringTokens.Tokens.Add(new TokenHelpItem("{left,n,str}", "Left n chars of str", "{left,3,Hello} → Hel"));
        stringTokens.Tokens.Add(new TokenHelpItem("{mid,n,m,str}", "Middle m chars from n", "{mid,2,2,Hello} → el"));
        stringTokens.Tokens.Add(new TokenHelpItem("{right,n,str}", "Right n chars of str", "{right,2,Hello} → lo"));
        stringTokens.Tokens.Add(new TokenHelpItem("{first,str}", "First word in str", "{first,Apple Banana} → Apple"));
        stringTokens.Tokens.Add(new TokenHelpItem("{last,str}", "Last word in str", "{last,Apple Banana} → Banana"));
        stringTokens.Tokens.Add(new TokenHelpItem("{upper,str}", "Convert to UPPER", "{upper,hello} → HELLO"));
        stringTokens.Tokens.Add(new TokenHelpItem("{lower,str}", "Convert to lower", "{lower,HELLO} → hello"));
        stringTokens.Tokens.Add(new TokenHelpItem("{capitalize,str}", "Capitalize first", "{capitalize,hELLO} → Hello"));
        stringTokens.Tokens.Add(new TokenHelpItem("{default,s1,s2}", "s1 or s2 if s1 empty", "{default,,Fallback} → Fallback"));
        stringTokens.Tokens.Add(new TokenHelpItem("{if,test,s1,s2}", "s1 if test empty", "{if,,A,B} → A"));
        stringTokens.Tokens.Add(new TokenHelpItem("{cc1,code}", "Country name", "{cc1,US} → United States"));
        stringTokens.Tokens.Add(new TokenHelpItem("{cc2,code}", "3-char to 2-char", "{cc2,USA} → US"));
        Categories.Add(stringTokens);
    }
}

public class TokenHelpCategory
{
    public string Name { get; }
    public ObservableCollection<TokenHelpItem> Tokens { get; } = [];

    public TokenHelpCategory(string name)
    {
        Name = name;
    }
}

public record TokenHelpItem(string Token, string Description, string Example);
