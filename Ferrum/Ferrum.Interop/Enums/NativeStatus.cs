namespace Ferrum.Interop;

/// <summary>Experimental ABI v1 status codes; not a stable plugin SDK.</summary>
public enum NativeStatus
{
    Ok = 0,
    InvalidArgument = 1,
    InvalidUtf8 = 2,
    CallbackError = 3,
    InternalError = 4,
    Panic = 5,
}
