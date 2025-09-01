
namespace Application.Common.Extensions;

public static class StringExtensions
{
   
    public static string RemoveWildcards(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return input.Replace("%", "").Replace("_", "");
    }

   
    public static string ToSlug(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var slug = input.ToLowerInvariant();

        var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(slug);
        slug = Encoding.ASCII.GetString(bytes);

        slug = Regex.Replace(slug, @"\s+", "-", RegexOptions.Compiled);

        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "", RegexOptions.Compiled);

        slug = Regex.Replace(slug, @"-+", "-", RegexOptions.Compiled);

        slug = slug.Trim('-');

        return slug;
    }

   
    public static string TruncateAtWord(this string input, int maxLength, string ellipsis = "...")
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        if (maxLength <= ellipsis.Length)
            return ellipsis;

        var truncated = input.Substring(0, maxLength - ellipsis.Length);
        var lastSpace = truncated.LastIndexOf(' ');

        if (lastSpace > 0)
        {
            truncated = truncated.Substring(0, lastSpace);
        }

        return truncated + ellipsis;
    }
}