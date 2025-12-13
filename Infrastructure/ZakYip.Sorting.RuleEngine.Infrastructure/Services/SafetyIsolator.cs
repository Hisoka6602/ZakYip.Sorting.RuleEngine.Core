using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services
{
    /// <summary>
    /// 安全隔离器，用于安全执行可能抛出异常的操作，支持同步和异步操作，并可选集成Polly弹性策略
    /// Safety Isolator for safely executing operations that may throw exceptions, supports both sync and async operations, with optional Polly resilience policy integration
    /// </summary>
    public class SafetyIsolator
    {
        private readonly ILogger _logger;
        private readonly ResiliencePipeline? _resiliencePipeline;

        public SafetyIsolator(ILogger logger, ResiliencePipeline? resiliencePipeline = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resiliencePipeline = resiliencePipeline;
        }

        /// <summary>
        /// 安全执行操作，返回是否成功
        /// Safely execute an operation, returns success status
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <returns>操作是否成功</returns>
        public bool Execute(Action action, string operationName)
        {
            try
            {
                if (_resiliencePipeline != null && action != null)
                {
                    _resiliencePipeline.Execute(() => action());
                }
                else
                {
                    action?.Invoke();
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安全隔离器捕获异常 - 操作: {OperationName} | Safety isolator caught exception - Operation: {OperationName}", operationName);
                return false;
            }
        }

        /// <summary>
        /// 安全执行操作，返回结果或默认值
        /// Safely execute an operation, returns result or default value
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="defaultValue">异常时返回的默认值</param>
        /// <returns>操作结果或默认值</returns>
        public T Execute<T>(Func<T> func, string operationName, T defaultValue = default)
        {
            try
            {
                if (_resiliencePipeline != null && func != null)
                {
                    return _resiliencePipeline.Execute(() => func());
                }
                return func != null ? func() : defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安全隔离器捕获异常 - 操作: {OperationName}，返回默认值 | Safety isolator caught exception - Operation: {OperationName}, returning default value", operationName);
                return defaultValue;
            }
        }

        /// <summary>
        /// 安全执行操作，返回操作结果和是否成功
        /// Safely execute an operation, returns result and success status
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="defaultValue">异常时返回的默认值</param>
        /// <returns>包含结果和成功状态的元组</returns>
        public (bool Success, T Result) TryExecute<T>(Func<T> func, string operationName, T defaultValue = default)
        {
            try
            {
                var result = func != null ? func() : defaultValue;
                return (true, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安全隔离器捕获异常 - 操作: {OperationName}，返回默认值 | Safety isolator caught exception - Operation: {OperationName}, returning default value", operationName);
                return (false, defaultValue);
            }
        }

        /// <summary>
        /// 安全执行操作，捕获但不记录异常（用于已知可能失败的尝试性操作）
        /// Safely execute an operation, catch but don't log exceptions (for known potentially failing tentative operations)
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <returns>操作是否成功</returns>
        public bool ExecuteSilent(Action action)
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 安全执行操作，捕获但不记录异常（用于已知可能失败的尝试性操作）
        /// Safely execute an operation, catch but don't log exceptions (for known potentially failing tentative operations)
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="defaultValue">异常时返回的默认值</param>
        /// <returns>操作结果或默认值</returns>
        public T ExecuteSilent<T>(Func<T> func, T defaultValue = default)
        {
            try
            {
                return func != null ? func() : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        #region Async Methods

        /// <summary>
        /// 异步安全执行操作，返回是否成功
        /// Asynchronously safely execute an operation, returns success status
        /// </summary>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <returns>操作是否成功</returns>
        public async Task<bool> ExecuteAsync(Func<Task> action, string operationName)
        {
            try
            {
                if (_resiliencePipeline != null && action != null)
                {
                    await _resiliencePipeline.ExecuteAsync(async (CancellationToken ct) => 
                    {
                        await action().ConfigureAwait(false);
                        return ValueTask.CompletedTask;
                    }, CancellationToken.None).ConfigureAwait(false);
                }
                else if (action != null)
                {
                    await action().ConfigureAwait(false);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安全隔离器捕获异常 - 操作: {OperationName} | Safety isolator caught exception - Operation: {OperationName}", operationName);
                return false;
            }
        }

        /// <summary>
        /// 异步安全执行操作，返回结果或默认值
        /// Asynchronously safely execute an operation, returns result or default value
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">要执行的异步函数</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="defaultValue">异常时返回的默认值</param>
        /// <returns>操作结果或默认值</returns>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> func, string operationName, T defaultValue = default)
        {
            try
            {
                if (_resiliencePipeline != null && func != null)
                {
                    return await _resiliencePipeline.ExecuteAsync(async (CancellationToken ct) => 
                    {
                        return await func().ConfigureAwait(false);
                    }, CancellationToken.None).ConfigureAwait(false);
                }
                return func != null ? await func().ConfigureAwait(false) : defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安全隔离器捕获异常 - 操作: {OperationName}，返回默认值 | Safety isolator caught exception - Operation: {OperationName}, returning default value", operationName);
                return defaultValue;
            }
        }

        /// <summary>
        /// 异步安全执行操作，返回操作结果和是否成功
        /// Asynchronously safely execute an operation, returns result and success status
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">要执行的异步函数</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="defaultValue">异常时返回的默认值</param>
        /// <returns>包含结果和成功状态的元组</returns>
        public async Task<(bool Success, T Result)> TryExecuteAsync<T>(Func<Task<T>> func, string operationName, T defaultValue = default)
        {
            try
            {
                var result = func != null ? await func().ConfigureAwait(false) : defaultValue;
                return (true, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安全隔离器捕获异常 - 操作: {OperationName}，返回默认值 | Safety isolator caught exception - Operation: {OperationName}, returning default value", operationName);
                return (false, defaultValue);
            }
        }

        /// <summary>
        /// 异步安全执行操作，捕获但不记录异常（用于已知可能失败的尝试性操作）
        /// Asynchronously safely execute an operation, catch but don't log exceptions (for known potentially failing tentative operations)
        /// </summary>
        /// <param name="action">要执行的异步操作</param>
        /// <returns>操作是否成功</returns>
        public async Task<bool> ExecuteSilentAsync(Func<Task> action)
        {
            try
            {
                if (action != null)
                {
                    await action().ConfigureAwait(false);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 异步安全执行操作，捕获但不记录异常（用于已知可能失败的尝试性操作）
        /// Asynchronously safely execute an operation, catch but don't log exceptions (for known potentially failing tentative operations)
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">要执行的异步函数</param>
        /// <param name="defaultValue">异常时返回的默认值</param>
        /// <returns>操作结果或默认值</returns>
        public async Task<T> ExecuteSilentAsync<T>(Func<Task<T>> func, T defaultValue = default)
        {
            try
            {
                return func != null ? await func().ConfigureAwait(false) : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion
    }
}
