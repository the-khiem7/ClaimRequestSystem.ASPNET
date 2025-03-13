using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;

namespace ClaimRequest.BLL.Aspects
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class LoggerAspect : OverrideMethodAspect
    {
        // Define compile-time parameters as strings to avoid runtime reference issues
        public string EntryLogLevel { get; set; } = "Debug";
        public string SuccessLogLevel { get; set; } = "Information";
        public string ExceptionLogLevel { get; set; } = "Error";
        public bool LogParameters { get; set; } = true;
        public bool LogReturnValue { get; set; } = true;

        public override void BuildAspect(IAspectBuilder<IMethod> builder)
        {
            // Verify that the containing type has a logger field
            builder.Advice.WithPriority(1000)
                .UsingNamespace("Microsoft.Extensions.Logging")
                .UsingNamespace("System.Text.Json")
                .UsingNamespace("System.Text.Json.Serialization");
        }

        public override dynamic? OverrideMethod()
        {
            var methodName = $"{meta.Target.Type.Name}.{meta.Target.Method.Name}";

            // Get logger from the instance (_logger field)
            ILogger logger = null!;
            var loggerField = meta.This.GetType().GetField("_logger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (loggerField != null)
            {
                logger = (ILogger)loggerField.GetValue(meta.This)!;
            }
            else
            {
                // Try to find any field of ILogger type
                var loggerFields = meta.This.GetType().GetFields(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .FirstOrDefault(f => typeof(ILogger).IsAssignableFrom(f.FieldType));

                if (loggerFields != null)
                {
                    logger = (ILogger)loggerFields.GetValue(meta.This)!;
                }
                else
                {
                    throw new InvalidOperationException($"No ILogger field found in {meta.This.GetType().Name}");
                }
            }

            // Parse log levels from strings
            LogLevel entryLogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), EntryLogLevel);
            LogLevel successLogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), SuccessLogLevel);
            LogLevel exceptionLogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), ExceptionLogLevel);

            // Log method entry with parameters if enabled
            if (LogParameters && logger.IsEnabled(entryLogLevel))
            {
                var parameters = meta.Target.Method.Parameters;
                if (parameters.Count > 0)
                {
                    try
                    {
                        var parameterValues = new object[parameters.Count];
                        for (int i = 0; i < parameters.Count; i++)
                        {
                            parameterValues[i] = meta.Arguments[parameters[i].Name];
                        }

                        var paramInfo = JsonSerializer.Serialize(parameterValues, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            MaxDepth = 3
                        });
                        logger.Log(entryLogLevel, "Entering {MethodName} with parameters: {Parameters}",
                            methodName, paramInfo);
                    }
                    catch (Exception ex)
                    {
                        logger.Log(entryLogLevel, ex,
                            "Entering {MethodName} with parameters (serialization failed)", methodName);
                    }
                }
                else
                {
                    logger.Log(entryLogLevel, "Entering {MethodName}", methodName);
                }
            }

            try
            {
                // Execute the actual method
                var result = meta.Proceed();

                // Log successful execution with return value if enabled
                if (LogReturnValue && logger.IsEnabled(successLogLevel) && result != null)
                {
                    try
                    {
                        var returnValue = JsonSerializer.Serialize(result, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            MaxDepth = 2
                        });
                        logger.Log(successLogLevel,
                            "Successfully executed {MethodName}, returned: {ReturnValue}",
                            methodName, returnValue);
                    }
                    catch (Exception ex)
                    {
                        logger.Log(successLogLevel, ex,
                            "Successfully executed {MethodName}, return value (serialization failed)",
                            methodName);
                    }
                }
                else
                {
                    logger.Log(successLogLevel, "Successfully executed {MethodName}", methodName);
                }

                return result;
            }
            catch (Exception ex)
            {
                // Log exception and rethrow
                logger.Log(exceptionLogLevel, ex,
                    "Exception in {MethodName}: {ExceptionMessage}",
                    methodName, ex.Message);
                throw;
            }
        }
    }
}
