using System.Text;

public static class Conversion
{
    private static readonly Dictionary<char, string> CyrillicToLatin = new()
    {
        {'а',"a"},{'б',"b"},{'в',"v"},{'г',"g"},{'д',"d"},
        {'е',"e"},{'ё',"yo"},{'ж',"zh"},{'з',"z"},{'и',"i"},
        {'й',"y"},{'к',"k"},{'л',"l"},{'м',"m"},{'н',"n"},
        {'о',"o"},{'п',"p"},{'р',"r"},{'с',"s"},{'т',"t"},
        {'у',"u"},{'ф',"f"},{'х',"kh"},{'ц',"ts"},{'ч',"ch"},
        {'ш',"sh"},{'щ',"sch"},{'ъ',"\""},{'ы',"y"},{'ь',"'"},
        {'э',"e"},{'ю',"yu"},{'я',"ya"},
        {'ө',"o"},{'ү',"u"},
        // Том үсэг
        {'А',"A"},{'Б',"B"},{'В',"V"},{'Г',"G"},{'Д',"D"},
        {'Е',"E"},{'Ё',"Yo"},{'Ж',"Zh"},{'З',"Z"},{'И',"I"},
        {'Й',"Y"},{'К',"K"},{'Л',"L"},{'М',"M"},{'Н',"N"},
        {'О',"O"},{'П',"P"},{'Р',"R"},{'С',"S"},{'Т',"T"},
        {'У',"U"},{'Ф',"F"},{'Х',"Kh"},{'Ц',"Ts"},{'Ч',"Ch"},
        {'Ш',"Sh"},{'Щ',"Sch"},{'Э',"E"},{'Ю',"Yu"},{'Я',"Ya"},
        {'Ө',"O"},{'Ү',"U"},
    };

    public static string ConvertStringToLatin(string input, int maxLength)
    {
        var result = new StringBuilder();
        foreach (char c in input)
        {
            if (CyrillicToLatin.TryGetValue(c, out var latin))
                result.Append(latin);
            else
                result.Append(c);
        }

        var converted = result.ToString();

        return converted.Length > maxLength
            ? converted[..maxLength]
            : converted;
    }

    public static string ConvertTextToHexString(string input)
    {
        var sb = new StringBuilder();
        foreach (char c in input)
            sb.Append(((int)c).ToString("X2"));
        return sb.ToString();
    }
}
