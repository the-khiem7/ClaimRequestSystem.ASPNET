using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClaimRequest.BLL.Aspects
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class LoggerAspect : OverrideMethodAspect
    {
        private readonly LogLevel _entryLevel;
        private readonly LogLevel _successLevel;
        private readonly LogLevel _exceptionLevel;
        private readonly bool _logParameters;
        private readonly bool _logReturnValue;

        public LoggerAspect(
            LogLevel entryLevel = LogLevel.Debug,
            LogLevel successLevel = LogLevel.Information,
            LogLevel exceptionLevel = LogLevel.Error,
            bool logParameters = true,
            bool logReturnValue = true)
        {
            _entryLevel = entryLevel;
            _successLevel = successLevel;
            _exceptionLevel = exceptionLevel;
            _logParameters = logParameters;
            _logReturnValue = logReturnValue;
        }

        public override dynamic? OverrideMethod()
        {
            var methodName = $"{meta.Target.Type.Name}.{meta.Target.Method.Name}";
            var logger = meta.This.GetLogger();

            if (_logParameters && logger.IsEnabled(_entryLevel))
            {
                var parameters = meta.Target.Method.Parameters;
                if (parameters.Count > 0)
                {
                    var paramValues = new object[parameters.Count];
                    for (var i = 0; i < parameters.Count; i++)
                    {
                        paramValues[i] = meta.Arguments[i];
                    }

                    try
                    {
                        var paramInfo = JsonSerializer.Serialize(paramValues, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            MaxDepth = 3
                        });
                        logger.Log(_entryLevel, "Entering {MethodName} with parameters: {Parameters}", methodName, paramInfo);
                    }
                    catch
                    {
                        logger.Log(_entryLevel, "Entering {MethodName} with parameters (serialization failed)", methodName);
                    }
                }
                else
                {
                    logger.Log(_entryLevel, "Entering {MethodName}", methodName);
                }
            }

            try
            {
                var result = meta.Proceed();

                if (_logReturnValue && logger.IsEnabled(_successLevel))
                {
                    if (result != null)
                    {
                        try
                        {
                            var returnValue = JsonSerializer.Serialize(result, new JsonSerializerOptions
                            {
                                WriteIndented = true,
                                MaxDepth = 2
                            });
                            logger.Log(_successLevel, "Successfully executed {MethodName}, returned: {ReturnValue}", methodName, returnValue);
                        }
                        catch
                        {
                            logger.Log(_successLevel, "Successfully executed {MethodName}, return value (serialization failed)", methodName);
                        }
                    }
                    else
                    {
                        logger.Log(_successLevel, "Successfully executed {MethodName}", methodName);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.Log(_exceptionLevel, ex, "Exception in {MethodName}: {ExceptionMessage}", methodName, ex.Message);
                throw;
            }
        }

        [CompileTime]
        private static ILogger GetLogger(object instance)
        {
            var type = instance.GetType();
            var loggerField = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.FieldType.IsAssignableTo(typeof(ILogger)) && f.Name.Contains("logger", StringComparison.OrdinalIgnoreCase));

            if (loggerField != null)
            {
                return (ILogger)loggerField.GetValue(instance)!;
            }

            // Default implementation for when no logger field is found
            var loggerFactoryField = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.FieldType == typeof(ILoggerFactory));

            if (loggerFactoryField != null)
            {
                var factory = (ILoggerFactory)loggerFactoryField.GetValue(instance)!;
                return factory.CreateLogger(type);
            }

            throw new InvalidOperationException($"No ILogger field found in {type.Name}");
        }
    }
}
