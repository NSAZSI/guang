namespace AdPulse.Api.Common;

/// <summary>业务异常：携带统一错误码与 HTTP 状态码，由全局异常中间件转成统一返回体。</summary>
public class ApiException : Exception
{
    public int Code { get; }
    public int HttpStatus { get; }

    public ApiException(int code, string message, int httpStatus = 400) : base(message)
    {
        Code = code;
        HttpStatus = httpStatus;
    }

    public static ApiException BadRequest(string message) => new(40000, message, 400);
    public static ApiException Unauthorized(string message = "未登录或登录已过期") => new(40100, message, 401);
    public static ApiException Forbidden(string message = "权限不足，无法执行该操作") => new(40300, message, 403);
    public static ApiException NotFound(string message = "资源不存在") => new(40400, message, 404);
}