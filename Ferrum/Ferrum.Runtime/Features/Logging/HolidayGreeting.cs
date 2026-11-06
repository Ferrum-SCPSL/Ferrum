using System;
using Ferrum.Contracts;

namespace Ferrum.Runtime.Features.Logging;

public static class HolidayGreeting
{
    public static string? GetMessage(DateTime date) => GetMessage(date, FerrumServices.Localization);
    public static string? GetMessage(DateTime date, ILocalizationService? messages)
    {
        if (date.Month == 10 && date.Day >= 25 && date.Day <= 31)
            return messages?.GetMessage("holiday.halloween") ?? "Spooky scary skeletons! Happy Halloween!";
        if ((date.Month == 12 && date.Day >= 25) || (date.Month == 1 && date.Day <= 7))
            return messages?.GetMessage("holiday.new_year") ?? "Happy New Year!";
        return null;
    }
}
