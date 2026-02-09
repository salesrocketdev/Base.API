using System.Text;

namespace Base.Core.Helpers;

/// <summary>
/// Helper class for generating avatar URLs using UI Avatars
/// </summary>
public static class AvatarHelper
{
    private const string UIAvatarsBaseUrl = "https://ui-avatars.com/api/";

    /// <summary>
    /// Generates a user avatar URL using UI Avatars
    /// </summary>
    /// <param name="userName">User name</param>
    /// <param name="size">Image size (default: 200)</param>
    /// <param name="background">Background color (default: random)</param>
    /// <param name="color">Text color (default: white)</param>
    /// <param name="format">Image format (default: png)</param>
    /// <returns>UI Avatars URL</returns>
    public static string GenerateUserAvatar(string userName, int size = 200, string? background = null, string color = "ffffff", string format = "png")
    {
        if (string.IsNullOrEmpty(userName))
        {
            return GenerateDefaultAvatar(size, background, color, format);
        }

        // Get initials from user name
        var initials = GetInitials(userName);

        // Generate random background color if not provided
        var bgColor = background ?? GenerateRandomColor();

        var parameters = new Dictionary<string, string>
        {
            ["name"] = initials,
            ["size"] = size.ToString(),
            ["background"] = bgColor,
            ["color"] = color,
            ["format"] = format,
            ["bold"] = "true",
            ["font-size"] = "0.5"
        };

        return BuildUrl(parameters);
    }

    /// <summary>
    /// Generates a default avatar for users without names
    /// </summary>
    /// <param name="size">Image size</param>
    /// <param name="background">Background color</param>
    /// <param name="color">Text color</param>
    /// <param name="format">Image format</param>
    /// <returns>Default avatar URL</returns>
    public static string GenerateDefaultAvatar(int size = 200, string? background = null, string color = "ffffff", string format = "png")
    {
        var bgColor = background ?? GenerateRandomColor();

        var parameters = new Dictionary<string, string>
        {
            ["name"] = "User",
            ["size"] = size.ToString(),
            ["background"] = bgColor,
            ["color"] = color,
            ["format"] = format,
            ["bold"] = "true",
            ["font-size"] = "0.5"
        };

        return BuildUrl(parameters);
    }

    /// <summary>
    /// Generates an avatar with custom text
    /// </summary>
    /// <param name="text">Custom text to display</param>
    /// <param name="size">Image size</param>
    /// <param name="background">Background color</param>
    /// <param name="color">Text color</param>
    /// <param name="format">Image format</param>
    /// <returns>Custom avatar URL</returns>
    public static string GenerateCustomAvatar(string text, int size = 200, string? background = null, string color = "ffffff", string format = "png")
    {
        var bgColor = background ?? GenerateRandomColor();

        var parameters = new Dictionary<string, string>
        {
            ["name"] = text,
            ["size"] = size.ToString(),
            ["background"] = bgColor,
            ["color"] = color,
            ["format"] = format,
            ["bold"] = "true",
            ["font-size"] = "0.5"
        };

        return BuildUrl(parameters);
    }

    /// <summary>
    /// Generates an avatar based on user role
    /// </summary>
    /// <param name="userName">User name</param>
    /// <param name="userRole">User role for color</param>
    /// <param name="size">Image size</param>
    /// <param name="color">Text color</param>
    /// <param name="format">Image format</param>
    /// <returns>User role avatar URL</returns>
    public static string GenerateUserRoleAvatar(string userName, string userRole, int size = 200, string color = "ffffff", string format = "png")
    {
        var initials = GetInitials(userName);
        var bgColor = GetUserRoleColor(userRole);

        var parameters = new Dictionary<string, string>
        {
            ["name"] = initials,
            ["size"] = size.ToString(),
            ["background"] = bgColor,
            ["color"] = color,
            ["format"] = format,
            ["bold"] = "true",
            ["font-size"] = "0.5"
        };

        return BuildUrl(parameters);
    }

    /// <summary>
    /// Gets initials from user name
    /// </summary>
    /// <param name="userName">User name</param>
    /// <returns>Initials string</returns>
    private static string GetInitials(string userName)
    {
        if (string.IsNullOrEmpty(userName))
            return "U";

        var words = userName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
            return "U";

        if (words.Length == 1)
            return words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpper();

        // Get first letter of first two words
        var firstInitial = words[0].Substring(0, 1).ToUpper();
        var secondInitial = words[1].Substring(0, 1).ToUpper();

        return $"{firstInitial}{secondInitial}";
    }

    /// <summary>
    /// Generates a random background color
    /// </summary>
    /// <returns>Random hex color</returns>
    private static string GenerateRandomColor()
    {
        var random = new Random();
        var colors = new[]
        {
            "FF6B6B", // Red
            "4ECDC4", // Teal
            "45B7D1", // Blue
            "96CEB4", // Green
            "FFEAA7", // Yellow
            "DDA0DD", // Plum
            "98D8C8", // Mint
            "F7DC6F", // Gold
            "BB8FCE", // Purple
            "85C1E9", // Light Blue
            "F8C471", // Orange
            "82E0AA", // Light Green
        };

        return colors[random.Next(colors.Length)];
    }

    /// <summary>
    /// Gets color based on user role
    /// </summary>
    /// <param name="userRole">User role</param>
    /// <returns>Color for user role</returns>
    private static string GetUserRoleColor(string userRole)
    {
        return userRole.ToLower() switch
        {
            "admin" => "FF6B6B", // Red
            "manager" => "4ECDC4", // Teal
            "sales" => "45B7D1", // Blue
            "support" => "96CEB4", // Green
            "user" => "BB8FCE", // Purple
            "guest" => "85C1E9", // Light Blue
            _ => GenerateRandomColor()
        };
    }

    /// <summary>
    /// Builds the UI Avatars URL with parameters
    /// </summary>
    /// <param name="parameters">URL parameters</param>
    /// <returns>Complete URL</returns>
    private static string BuildUrl(Dictionary<string, string> parameters)
    {
        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{UIAvatarsBaseUrl}?{queryString}";
    }

    /// <summary>
    /// Validates if a URL is a UI Avatars URL
    /// </summary>
    /// <param name="url">URL to validate</param>
    /// <returns>True if it's a UI Avatars URL</returns>
    public static bool IsUIAvatarsUrl(string url)
    {
        return !string.IsNullOrEmpty(url) && url.StartsWith(UIAvatarsBaseUrl, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts user name from UI Avatars URL
    /// </summary>
    /// <param name="url">UI Avatars URL</param>
    /// <returns>User name or null if not found</returns>
    public static string? ExtractUserNameFromUrl(string url)
    {
        if (!IsUIAvatarsUrl(url))
            return null;

        try
        {
            var uri = new Uri(url);
            var query = uri.Query;
            var nameParam = query.Split('&')
                .FirstOrDefault(p => p.StartsWith("name="));

            if (nameParam != null)
            {
                return Uri.UnescapeDataString(nameParam.Substring(5));
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
