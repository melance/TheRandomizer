using System.Text.RegularExpressions;

namespace TheRandomizer.Table;

internal partial class OutputParser
{
    private static readonly Regex TokenRegex = _TokenRegex();

    public static String Replace(String template, Func<String, String> resolver)
    {
        return TokenRegex.Replace(template, match =>
        {
            var token = match.Groups[1].Value.Trim();
            return resolver(token);
        });
    }

    [GeneratedRegex(@"\[([^\[\]]+)\]")]
    private static partial Regex _TokenRegex();
}

