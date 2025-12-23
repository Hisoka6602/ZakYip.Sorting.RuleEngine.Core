using Microsoft.Extensions.Logging;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Utilities;

/// <summary>
/// 事件处理器扩展方法 - 安全调用事件，防止订阅者异常影响其他订阅者
/// Event handler extension methods - safely invoke events, preventing subscriber exceptions from affecting others
/// </summary>
/// <remarks>
/// 参考 ZakYip.WheelDiverterSorter 项目的实现
/// Referenced from ZakYip.WheelDiverterSorter project implementation
/// </remarks>
public static class EventHandlerExtensions
{
    /// <summary>
    /// 安全调用事件 - 捕获并记录每个订阅者的异常，但不阻止其他订阅者执行
    /// Safely invoke event - catch and log each subscriber's exception without blocking others
    /// </summary>
    /// <typeparam name="TEventArgs">事件参数类型 / Event args type</typeparam>
    /// <param name="eventHandler">事件处理器 / Event handler</param>
    /// <param name="sender">事件发送者 / Event sender</param>
    /// <param name="args">事件参数 / Event arguments</param>
    /// <param name="logger">日志记录器 / Logger</param>
    /// <param name="eventName">事件名称（用于日志） / Event name (for logging)</param>
    /// <remarks>
    /// 此方法遍历所有订阅者并逐个调用，捕获每个订阅者的异常而不影响其他订阅者
    /// This method iterates through all subscribers and invokes them individually,
    /// catching each subscriber's exception without affecting others
    /// 
    /// 使用场景 / Use cases:
    /// - 事件发布者想要确保所有订阅者都能收到事件，即使某些订阅者抛出异常
    /// - Event publisher wants to ensure all subscribers receive the event, even if some throw exceptions
    /// - 需要记录哪个订阅者抛出了异常，便于调试
    /// - Need to log which subscriber threw an exception for debugging
    /// </remarks>
    public static void SafeInvoke<TEventArgs>(
        this EventHandler<TEventArgs>? eventHandler,
        object? sender,
        TEventArgs args,
        ILogger? logger = null,
        string? eventName = null)
    {
        if (eventHandler == null)
        {
            return;
        }

        var invocationList = eventHandler.GetInvocationList();
        var eventNameDisplay = eventName ?? typeof(TEventArgs).Name;

        foreach (var handler in invocationList)
        {
            try
            {
                ((EventHandler<TEventArgs>)handler).Invoke(sender, args);
            }
            catch (Exception ex)
            {
                // Log the exception but continue invoking other subscribers
                logger?.LogError(
                    ex,
                    "订阅者处理事件 '{EventName}' 时发生异常 / Subscriber threw exception while handling event '{EventName}': Target={Target}, Method={Method}",
                    eventNameDisplay,
                    handler.Target?.GetType().Name ?? "Unknown",
                    handler.Method.Name);
            }
        }
    }

    /// <summary>
    /// 安全调用简单事件（无参数）
    /// Safely invoke simple event (no arguments)
    /// </summary>
    /// <param name="eventHandler">事件处理器 / Event handler</param>
    /// <param name="sender">事件发送者 / Event sender</param>
    /// <param name="logger">日志记录器（可选） / Logger (optional)</param>
    /// <param name="eventName">事件名称（用于日志） / Event name (for logging)</param>
    public static void SafeInvoke(
        this EventHandler? eventHandler,
        object? sender,
        ILogger? logger = null,
        string? eventName = null)
    {
        if (eventHandler == null)
        {
            return;
        }

        var eventNameDisplay = eventName ?? "EventHandler";
        
        foreach (var del in eventHandler.GetInvocationList())
        {
            var originalDelegate = (EventHandler)del;
            try
            {
                originalDelegate.Invoke(sender, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                logger?.LogError(
                    ex,
                    "订阅者处理事件 '{EventName}' 时发生异常 / Subscriber threw exception while handling event '{EventName}': Target={Target}, Method={Method}",
                    eventNameDisplay,
                    del.Target?.GetType().Name ?? "Unknown",
                    del.Method.Name);
            }
        }
    }

    /// <summary>
    /// 安全调用异步事件委托（Func&lt;T, Task&gt;）- 捕获并记录每个订阅者的异常，但不阻止其他订阅者执行
    /// Safely invoke async event delegate (Func&lt;T, Task&gt;) - catch and log each subscriber's exception without blocking others
    /// </summary>
    /// <typeparam name="T">事件参数类型 / Event argument type</typeparam>
    /// <param name="asyncEventHandler">异步事件处理器 / Async event handler</param>
    /// <param name="args">事件参数 / Event argument</param>
    /// <param name="logger">日志记录器（可选） / Logger (optional)</param>
    /// <param name="eventName">事件名称（用于日志） / Event name (for logging)</param>
    /// <remarks>
    /// 此方法用于安全调用 Func&lt;T, Task&gt; 类型的异步事件委托（如 DWS 数据接收事件）
    /// This method is used to safely invoke Func&lt;T, Task&gt; type async event delegates (e.g., DWS data received event)
    /// 
    /// 使用场景 / Use cases:
    /// - DWS 适配器接收到数据后触发事件，防止订阅者异常导致适配器崩溃
    /// - DWS adapter triggers event after receiving data, preventing subscriber exceptions from crashing the adapter
    /// - 任何使用 Func&lt;T, Task&gt; 类型异步委托的场景
    /// - Any scenario using Func&lt;T, Task&gt; type async delegates
    /// </remarks>
    public static async Task SafeInvokeAsync<T>(
        this Func<T, Task>? asyncEventHandler,
        T args,
        ILogger? logger = null,
        string? eventName = null)
    {
        if (asyncEventHandler == null)
        {
            return;
        }

        var invocationList = asyncEventHandler.GetInvocationList();
        var eventNameDisplay = eventName ?? typeof(T).Name;

        foreach (var handler in invocationList)
        {
            try
            {
                await ((Func<T, Task>)handler).Invoke(args).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log the exception but continue invoking other subscribers
                logger?.LogError(
                    ex,
                    "订阅者处理异步事件 '{EventName}' 时发生异常 / Subscriber threw exception while handling async event '{EventName}': Target={Target}, Method={Method}",
                    eventNameDisplay,
                    handler.Target?.GetType().Name ?? "Unknown",
                    handler.Method.Name);
            }
        }
    }
}
