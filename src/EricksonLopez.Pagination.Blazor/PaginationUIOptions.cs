// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.Pagination.Blazor;

/// <summary>
/// Represents options for configuring default CSS classes used by Blazor pagination components.
/// </summary>
public class PaginationUIOptions
{
    /// <summary>
    /// Gets or sets additional CSS classes for the navigation container element.
    /// </summary>
    public string ContainerClass { get; set; } = "";

    /// <summary>
    /// Gets or sets additional CSS classes for the pagination container element.
    /// </summary>
    public string PaginationClass { get; set; } = "";

    /// <summary>
    /// Gets or sets the CSS class for the unordered list element.
    /// </summary>
    public string ListClass { get; set; } = "";

    /// <summary>
    /// Gets or sets the CSS class for list item elements.
    /// </summary>
    public string ItemClass { get; set; } = "";

    /// <summary>
    /// Gets or sets the CSS class for navigation link and button elements.
    /// </summary>
    public string LinkClass { get; set; } = "";

    /// <summary>
    /// Gets or sets the CSS class applied to the active page item.
    /// </summary>
    public string ActiveClass { get; set; } = "";

    /// <summary>
    /// Gets or sets the CSS class applied to disabled page items.
    /// </summary>
    public string DisabledClass { get; set; } = "";

    /// <summary>
    /// Gets a predefined set of UI options configured for Bootstrap.
    /// </summary>
    public static PaginationUIOptions Bootstrap => new()
    {
        PaginationClass = "justify-content-center",
        ListClass = "pagination",
        ItemClass = "page-item",
        LinkClass = "page-link",
        ActiveClass = "active",
        DisabledClass = "disabled"
    };

    /// <summary>
    /// Gets a predefined set of UI options configured for Tailwind CSS.
    /// </summary>
    public static PaginationUIOptions Tailwind => new()
    {
        PaginationClass = "flex justify-center",
        ListClass = "flex list-none rounded",
        ItemClass = "mx-1",
        LinkClass = "block px-3 py-2 border rounded hover:bg-gray-200",
        ActiveClass = "bg-blue-500 text-white",
        DisabledClass = "opacity-50 cursor-not-allowed"
    };

    /// <summary>
    /// Gets a predefined set of UI options configured for Bootstrap 5.
    /// </summary>
    public static PaginationUIOptions Bootstrap5 => new();
}
