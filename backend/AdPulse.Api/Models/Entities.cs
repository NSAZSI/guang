namespace AdPulse.Api.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Editor = "Editor";
}

public static class NewsStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Recycled = "Recycled";
}

public static class AiStatus
{
    public const string None = "None";
    public const string Pending = "Pending";
    public const string Done = "Done";
    public const string Fallback = "Fallback";
    public const string Failed = "Failed";
}

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = Roles.Editor;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int SortOrder { get; set; }
    public List<News> News { get; set; } = new();
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<NewsTag> NewsTags { get; set; } = new();
}

public class News
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Content { get; set; } = "";
    public string CoverUrl { get; set; } = "";
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public int AuthorId { get; set; }
    public User? Author { get; set; }
    public string Status { get; set; } = NewsStatus.Draft;
    public bool IsTop { get; set; }
    public bool IsHeadline { get; set; }
    public int ViewCount { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? AiSummary { get; set; }
    public string? AiTagsJson { get; set; }
    public string AiStatus { get; set; } = Models.AiStatus.None;
    public List<NewsTag> NewsTags { get; set; } = new();
}

public class NewsTag
{
    public int NewsId { get; set; }
    public News? News { get; set; }
    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}

public class Banner
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = "";
    public int? LinkNewsId { get; set; }
    public News? LinkNews { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class ViewLog
{
    public int Id { get; set; }
    public int NewsId { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}

public class OperationLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public string Action { get; set; } = "";
    public string Target { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}