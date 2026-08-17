namespace KiddoCare.Web.Extensions;

using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

public static class HtmlEnumExtensions
{
    public static IEnumerable<SelectListItem> GetLocalizedEnumSelectList<TEnum>(this IHtmlHelper htmlHelper, IStringLocalizer localizer) where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>()
            .Select(enumValue => new SelectListItem
            {
                Value = Convert.ToInt32(enumValue, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
                Text = localizer[enumValue.ToString()]
            });
    }
}
