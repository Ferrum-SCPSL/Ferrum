using System;

namespace Ferrum.Loader.Features.Logging;

/// <summary>Chooses a seasonal greeting from the server's local calendar date.</summary>
public static class HolidayGreeting
{
    public static string? GetMessage(DateTime date)
    {
        if (date.Month == 10 && date.Day >= 25 && date.Day <= 31)
            return "Spooky scary skeletons! Happy Halloween!";

        if ((date.Month == 12 && date.Day >= 25)
            || (date.Month == 1 && date.Day <= 7))
            return "Happy New Year!";

        return null;
    }
}
