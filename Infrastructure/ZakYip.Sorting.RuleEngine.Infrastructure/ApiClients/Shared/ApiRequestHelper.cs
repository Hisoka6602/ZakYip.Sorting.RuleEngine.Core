using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

/// <summary>
/// API请求辅助类，用于生成Curl命令和格式化请求信息
/// API Request Helper for generating Curl commands and formatting request information
/// </summary>
public static class ApiRequestHelper
{
    /// <summary>
    /// 异常嵌套追踪的最大深度
    /// Maximum depth for exception nesting tracking
    /// </summary>
    private const int MaxExceptionDepth = 10;

    /// <summary>
    /// 获取详细的异常信息，包括异常类型和所有内部异常
    /// Get detailed exception message including exception types and all inner exceptions
    /// </summary>
    /// <param name="exception">
    /// 异常对象；如果为 null，则返回空字符串。
    /// Exception object; if null, an empty string is returned.
    /// </param>
    /// <returns>
    /// 详细的异常信息，格式：[异常类型] 消息 --> [内部异常类型] 内部消息
    /// Detailed exception message in format: [ExceptionType] Message --> [InnerExceptionType] Inner Message
    /// </returns>
    public static string GetDetailedExceptionMessage(Exception? exception)
    {
        if (exception == null)
        {
            return string.Empty;
        }

        var messages = new StringBuilder();
        var currentException = exception;
        var depth = 0;

        while (currentException != null)
        {
            if (depth > 0)
            {
                messages.Append(" --> ");
            }

            // 包含异常类型，使错误信息更清晰明确
            // Include exception type to make error message clearer and more specific
            messages.Append('[');
            messages.Append(currentException.GetType().Name);
            messages.Append("] ");
            messages.Append(currentException.Message);

            currentException = currentException.InnerException;
            depth++;

            // 防止无限循环，最多追踪指定层数
            // Prevent infinite loop, max specified levels
            if (depth >= MaxExceptionDepth)
            {
                break;
            }
        }

        return messages.ToString();
    }

    /// <summary>
    /// 生成格式化的Curl命令
    /// Generate formatted Curl command
    /// </summary>
    /// <param name="method">HTTP方法</param>
    /// <param name="url">请求URL</param>
    /// <param name="headers">请求头字典</param>
    /// <param name="body">请求体</param>
    /// <returns>格式化的Curl命令字符串</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GenerateFormattedCurl(
        string method,
        string url,
        Dictionary<string, string>? headers = null,
        string? body = null)
    {
        var curlBuilder = new StringBuilder();
        curlBuilder.Append($"curl -X {method.ToUpper()} '{url}'");

        // 添加请求头
        if (headers != null)
        {
            foreach (var header in headers)
            {
                curlBuilder.Append($" \\\n  -H '{header.Key}: {header.Value}'");
            }
        }

        // 添加请求体
        if (!string.IsNullOrEmpty(body))
        {
            // 转义单引号
            var escapedBody = body.Replace("'", "'\\''", StringComparison.Ordinal);
            curlBuilder.Append($" \\\n  -d '{escapedBody}'");
        }

        return curlBuilder.ToString();
    }

    /// <summary>
    /// 从HttpRequestMessage生成格式化的Curl命令
    /// Generate formatted Curl command from HttpRequestMessage
    /// </summary>
    public static async Task<string> GenerateFormattedCurlFromRequestAsync(HttpRequestMessage request)
    {
        var headers = new Dictionary<string, string>();
        
        // 添加请求头
        foreach (var header in request.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }
        
        // 添加Content头
        if (request.Content?.Headers != null)
        {
            foreach (var header in request.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }

        // 读取请求体
        string? body = null;
        if (request.Content != null)
        {
            body = await request.Content.ReadAsStringAsync();
        }

        var url = request.RequestUri?.ToString() ?? "";
        var method = request.Method.Method;

        return GenerateFormattedCurl(method, url, headers, body);
    }

    /// <summary>
    /// 格式化请求头为JSON字符串
    /// Format request headers as JSON string
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormatHeaders(Dictionary<string, string> headers)
    {
        return JsonSerializer.Serialize(headers, new JsonSerializerOptions 
        { 
            WriteIndented = false 
        });
    }

    /// <summary>
    /// 从HttpRequestMessage获取格式化的请求头
    /// Get formatted request headers from HttpRequestMessage
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetFormattedHeadersFromRequest(HttpRequestMessage request)
    {
        var headers = new Dictionary<string, string>();
        
        foreach (var header in request.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }
        
        if (request.Content?.Headers != null)
        {
            foreach (var header in request.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }

        return FormatHeaders(headers);
    }

    /// <summary>
    /// 从HttpResponseMessage获取格式化的响应头
    /// Get formatted response headers from HttpResponseMessage
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetFormattedHeadersFromResponse(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>();
        
        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }
        
        if (response.Content?.Headers != null)
        {
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }

        return FormatHeaders(headers);
    }
}
