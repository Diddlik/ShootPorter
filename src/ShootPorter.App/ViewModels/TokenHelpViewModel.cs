using ShootPorter.App.Properties;
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
        var dateTimeTokens = new TokenHelpCategory(Resources.TokenCategory_DateAndTime);
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{d}", Resources.Token_d, "260329"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{t}", Resources.Token_t, "160922"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{x}", Resources.Token_x, "29.03.2026"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{X}", Resources.Token_XU, "16_09_22"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{y}", Resources.Token_y, "26"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{Y}", Resources.Token_YU, "2026"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{m}", Resources.Token_m, "03"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{b}", Resources.Token_b, "Mar"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{B}", Resources.Token_BU, "March"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{D}", Resources.Token_DU, "29"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{j}", Resources.Token_j, "088"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{W}", Resources.Token_WU, "12"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{WI}", Resources.Token_WUIU, "13"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{IWD}", Resources.Token_IUWUDU, "2026-W13-7"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{a}", Resources.Token_a, "Sun"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{A}", Resources.Token_AU, "Sunday"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{P}", Resources.Token_PU, "1"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{H}", Resources.Token_HU, "16"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{I}", Resources.Token_IU, "04"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{M}", Resources.Token_MU, "09"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{S}", Resources.Token_SU, "22"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{s}", Resources.Token_s, "00"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{p}", Resources.Token_p, "PM"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{plc}", Resources.Token_plc, "pm"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{z}", Resources.Token_z, "W. Europe..."));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{Z}", Resources.Token_ZU, "+0200"));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{#c}", Resources.Token_HUashc, "Sunday, 29 Mar..."));
        dateTimeTokens.Tokens.Add(new TokenHelpItem("{#x}", Resources.Token_HUashx, "Sunday, 29 Mar..."));
        Categories.Add(dateTimeTokens);

        // Current Time Tokens
        var nowTokens = new TokenHelpCategory(Resources.TokenCategory_CurrentTimeNow);
        nowTokens.Tokens.Add(new TokenHelpItem("{1}", Resources.Token_1, "2026"));
        nowTokens.Tokens.Add(new TokenHelpItem("{2}", Resources.Token_2, "03"));
        nowTokens.Tokens.Add(new TokenHelpItem("{3}", Resources.Token_3, "29"));
        nowTokens.Tokens.Add(new TokenHelpItem("{4}", Resources.Token_4, "26"));
        nowTokens.Tokens.Add(new TokenHelpItem("{5}", Resources.Token_5, "2026"));
        nowTokens.Tokens.Add(new TokenHelpItem("{6}", Resources.Token_6, "03"));
        nowTokens.Tokens.Add(new TokenHelpItem("{7}", Resources.Token_7, "29"));
        Categories.Add(nowTokens);

        // Camera/Shooting Data Tokens
        var cameraTokens = new TokenHelpCategory(Resources.TokenCategory_CameraShootingData);
        cameraTokens.Tokens.Add(new TokenHelpItem("{c}", Resources.Token_c, "543467"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{C}", Resources.Token_CU, "AEB"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{i}", Resources.Token_i, "100"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{k}", Resources.Token_k, "100"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{K1}", Resources.Token_KU1, "28"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{K2}", Resources.Token_KU2, "5.6"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{K3}", Resources.Token_KU3, "125"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{O}", Resources.Token_OU, "John Smith"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{copyright}", Resources.Token_copyright, "(C) 2026 John"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T}", Resources.Token_TU, "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T1}", Resources.Token_TU1, "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T2}", Resources.Token_TU2, "Canon EOS 50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T3}", Resources.Token_TU3, "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T4}", Resources.Token_TU4, "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T5}", Resources.Token_TU5, "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T6}", Resources.Token_TU6, "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T8}", Resources.Token_TU8, "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{T9}", Resources.Token_TU9, "50D"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{r8}", Resources.Token_r8, "12345"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{r85}", Resources.Token_r85, "12345"));
        cameraTokens.Tokens.Add(new TokenHelpItem("{r86}", Resources.Token_r86, "012345"));
        Categories.Add(cameraTokens);

        // File System Tokens
        var fileTokens = new TokenHelpCategory(Resources.TokenCategory_FileSystem);
        fileTokens.Tokens.Add(new TokenHelpItem("{e}", Resources.Token_e, "JPG"));
        fileTokens.Tokens.Add(new TokenHelpItem("{E}", Resources.Token_EU, "JPG"));
        fileTokens.Tokens.Add(new TokenHelpItem("{E1}", Resources.Token_EU1, ""));
        fileTokens.Tokens.Add(new TokenHelpItem("{E2}", Resources.Token_EU2, "JPG"));
        fileTokens.Tokens.Add(new TokenHelpItem("{f}", Resources.Token_f, "IMG"));
        fileTokens.Tokens.Add(new TokenHelpItem("{F}", Resources.Token_FU, "IMG_0123"));
        fileTokens.Tokens.Add(new TokenHelpItem("{file[2-5]}", Resources.Token_file_2_5, "G_01"));
        fileTokens.Tokens.Add(new TokenHelpItem("{file[2]}", Resources.Token_file_2, "G"));
        fileTokens.Tokens.Add(new TokenHelpItem("{file[4-]}", Resources.Token_file_4, "0123"));
        fileTokens.Tokens.Add(new TokenHelpItem("{o}", Resources.Token_o, "100CANON"));
        fileTokens.Tokens.Add(new TokenHelpItem("{q}", Resources.Token_q, "100"));
        fileTokens.Tokens.Add(new TokenHelpItem("{r}", Resources.Token_r, "0123"));
        fileTokens.Tokens.Add(new TokenHelpItem("{r1}", Resources.Token_r1, "3"));
        fileTokens.Tokens.Add(new TokenHelpItem("{r2}", Resources.Token_r2, "23"));
        fileTokens.Tokens.Add(new TokenHelpItem("{r3}", Resources.Token_r3, "123"));
        fileTokens.Tokens.Add(new TokenHelpItem("{r4}", Resources.Token_r4, "0123"));
        fileTokens.Tokens.Add(new TokenHelpItem("{u}", Resources.Token_u, "C:\\Users\\..."));
        Categories.Add(fileTokens);

        // Job Code and Numbering
        var jobTokens = new TokenHelpCategory(Resources.TokenCategory_JobCodeAndNumbering);
        jobTokens.Tokens.Add(new TokenHelpItem("{J}", Resources.Token_JU, "Wedding"));
        jobTokens.Tokens.Add(new TokenHelpItem("{Jc_fw}", Resources.Token_JUc_fw, "Wedding"));
        jobTokens.Tokens.Add(new TokenHelpItem("{l}", Resources.Token_l, ""));
        jobTokens.Tokens.Add(new TokenHelpItem("{l2}", Resources.Token_l2, "01"));
        jobTokens.Tokens.Add(new TokenHelpItem("{l3}", Resources.Token_l3, "001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{l4}", Resources.Token_l4, "0001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{L}", Resources.Token_LU, "1"));
        jobTokens.Tokens.Add(new TokenHelpItem("{seq#}", Resources.Token_seqHUash, "1"));
        jobTokens.Tokens.Add(new TokenHelpItem("{seq#2}", Resources.Token_seqHUash2, "01"));
        jobTokens.Tokens.Add(new TokenHelpItem("{seq#3}", Resources.Token_seqHUash3, "001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{seq#4}", Resources.Token_seqHUash4, "0001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{seq#5}", Resources.Token_seqHUash5, "00001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{seq#6}", Resources.Token_seqHUash6, "000001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{n1}", Resources.Token_n1, "1"));
        jobTokens.Tokens.Add(new TokenHelpItem("{n2}", Resources.Token_n2, "01"));
        jobTokens.Tokens.Add(new TokenHelpItem("{n3}", Resources.Token_n3, "001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{n4}", Resources.Token_n4, "0001"));
        jobTokens.Tokens.Add(new TokenHelpItem("{R}", Resources.Token_RU, "42"));
        Categories.Add(jobTokens);

        // String Functions
        var stringTokens = new TokenHelpCategory(Resources.TokenCategory_StringFunctions);
        stringTokens.Tokens.Add(new TokenHelpItem("{left,n,str}", Resources.Token_left_n_str, "{left,3,Hello} → Hel"));
        stringTokens.Tokens.Add(new TokenHelpItem("{mid,n,m,str}", Resources.Token_mid_n_m_str, "{mid,2,2,Hello} → el"));
        stringTokens.Tokens.Add(new TokenHelpItem("{right,n,str}", Resources.Token_right_n_str, "{right,2,Hello} → lo"));
        stringTokens.Tokens.Add(new TokenHelpItem("{first,str}", Resources.Token_first_str, "{first,Apple Banana} → Apple"));
        stringTokens.Tokens.Add(new TokenHelpItem("{last,str}", Resources.Token_last_str, "{last,Apple Banana} → Banana"));
        stringTokens.Tokens.Add(new TokenHelpItem("{upper,str}", Resources.Token_upper_str, "{upper,hello} → HELLO"));
        stringTokens.Tokens.Add(new TokenHelpItem("{lower,str}", Resources.Token_lower_str, "{lower,HELLO} → hello"));
        stringTokens.Tokens.Add(new TokenHelpItem("{capitalize,str}", Resources.Token_capitalize_str, "{capitalize,hELLO} → Hello"));
        stringTokens.Tokens.Add(new TokenHelpItem("{default,s1,s2}", Resources.Token_default_s1_s2, "{default,,Fallback} → Fallback"));
        stringTokens.Tokens.Add(new TokenHelpItem("{if,test,s1,s2}", Resources.Token_if_test_s1_s2, "{if,,A,B} → A"));
        stringTokens.Tokens.Add(new TokenHelpItem("{cc1,code}", Resources.Token_cc1_code, "{cc1,US} → United States"));
        stringTokens.Tokens.Add(new TokenHelpItem("{cc2,code}", Resources.Token_cc2_code, "{cc2,USA} → US"));
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
