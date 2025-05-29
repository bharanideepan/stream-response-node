using System;
using System.Collections.Generic;
using System.Text;

namespace AS_Auth;

public static class GuidProvider
{
    private static Func<Guid> _newGuidFunc = () => Guid.NewGuid();
    
    public static Guid NewGuid() => _newGuidFunc();

    public static void SetNewGuid(Func<Guid> newGuidFunc) =>
        _newGuidFunc = newGuidFunc;

    public static void ResetNewGuid() =>
        _newGuidFunc = () => Guid.NewGuid();
}
