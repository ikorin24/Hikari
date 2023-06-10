#nullable enable
using System;
using System.Text;

namespace Elffy;

internal sealed class EngineCoreException : Exception
{
    //private readonly NativeError[] _errors;

    //public ReadOnlyMemory<NativeError> Errors => _errors;

    //internal static EngineCoreException NewUnknownError() => new(ReadOnlySpan<NativeError>.Empty);
    //internal EngineCoreException(ReadOnlySpan<NativeError> errors) : base(BuildExceptionMessage(errors))
    //{
    //    _errors = errors.ToArray();
    //}

    //private static string BuildExceptionMessage(ReadOnlySpan<NativeError> errors)
    //{
    //    if(errors.Length == 0) {
    //        return "Some error occurred in the native code, but the error message could not be retrieved.";
    //    }
    //    if(errors.Length == 1) {
    //        return errors[0].Message;
    //    }
    //    else {
    //        var sb = new StringBuilder($"Multiple errors occurred in the native code. (ErrorCount: {errors.Length}) \n");
    //        foreach(var err in errors) {
    //            sb.AppendLine(err.Message);
    //        }
    //        return sb.ToString();
    //    }
    //}

    public EngineCoreException(string message) : base(message)
    {
    }
}

//internal record struct NativeError(CE.ErrMessageId Id, string Message);
