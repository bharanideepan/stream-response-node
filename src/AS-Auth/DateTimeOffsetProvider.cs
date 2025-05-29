using System;
using System.Collections.Generic;
using System.Text;

namespace AS_Auth;

public static class DateTimeOffsetProvider
{
    private static Func<DateTimeOffset> _utcNowFunc = () => DateTimeOffset.UtcNow;
    public static DateTimeOffset UtcNow => _utcNowFunc();

    public static void SetUtcNow(Func<DateTimeOffset> utcNowFunc) =>
        _utcNowFunc = utcNowFunc;

    public static void ResetUtcNow() =>
        _utcNowFunc = () => DateTimeOffset.UtcNow;
}
