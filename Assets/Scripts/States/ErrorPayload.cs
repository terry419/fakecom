using System;

// 상태 전환 시 예외 정보를 전달하기 위한 Payload
public class ErrorPayload : StatePayload
{
    public readonly Exception Exception;

    public ErrorPayload(Exception ex)
    {
        // null 예외를 전달하지 않도록 방어
        Exception = ex ?? new Exception("An unknown error occurred.");
    }
}
