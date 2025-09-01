
namespace Application.Common.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Xóa các ký tự wildcard (% và _) thường dùng trong tìm kiếm SQL.
    /// </summary>
    public static string RemoveWildcards(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return input.Replace("%", "").Replace("_", "");
    }

    /// <summary>
    /// Chuyển đổi một chuỗi thành dạng URL-friendly slug.
    /// Ví dụ: "Lập Trình .NET Core!" -> "lap-trinh-net-core"
    /// </summary>
    public static string ToSlug(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Chuyển sang chữ thường
        var slug = input.ToLowerInvariant();

        // Xóa dấu tiếng Việt
        var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(slug);
        slug = Encoding.ASCII.GetString(bytes);

        // Thay thế khoảng trắng và các ký tự phân cách bằng dấu gạch ngang
        slug = Regex.Replace(slug, @"\s+", "-", RegexOptions.Compiled);

        // Xóa các ký tự không hợp lệ
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "", RegexOptions.Compiled);

        // Xóa các dấu gạch ngang bị trùng lặp
        slug = Regex.Replace(slug, @"-+", "-", RegexOptions.Compiled);

        // Xóa dấu gạch ngang ở đầu và cuối chuỗi
        slug = slug.Trim('-');

        return slug;
    }

    /// <summary>
    /// Cắt ngắn chuỗi đến độ dài tối đa mà không cắt giữa chừng một từ.
    /// </summary>
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