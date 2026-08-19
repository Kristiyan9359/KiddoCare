namespace KiddoCare.Web.Extensions;

using Microsoft.AspNetCore.Mvc;

public static class ControllerNotificationExtensions
{
    public const string SuccessMessageKey = "SuccessMessage";
    public const string ErrorMessageKey = "ErrorMessage";

    public static void SetSuccessMessage(this Controller controller, string message)
    {
        controller.TempData[SuccessMessageKey] = message;
    }

    public static void SetErrorMessage(this Controller controller, string message)
    {
        controller.TempData[ErrorMessageKey] = message;
    }
}
