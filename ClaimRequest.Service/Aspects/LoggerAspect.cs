using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
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
            // Add required namespaces
            builder.Advice.AddNamespaces(
                "Microsoft.Extensions.Logging",
                "System.Text.Json",
                "System.Reflection");
        }

        public override dynamic? OverrideMethod()
        {
            var methodName = $"{meta.Target.Type.Name}.{meta.Target.Method.Name}";

            // Get logger using reflection
            ILogger logger = null!;
            var type = meta.This.GetType();
            var loggerField = type.GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);

            if (loggerField != null)
            {
                logger = (ILogger)loggerField.GetValue(meta.This)!;
            }
            else
            {
                // Try to find any field that implements ILogger
                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (typeof(ILogger).IsAssignableFrom(field.FieldType))
                    {
                        logger = (ILogger)field.GetValue(meta.This)!;
                        break;
                    }
                }

                if (logger == null)
                {
                    throw new InvalidOperationException($"No ILogger field found in {type.Name}");
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
                        // Create array to hold parameter values
                        var parameterValues = new object[parameters.Count];

                        // Capture parameter values using reflection since meta.Arguments is not available
                        var args = meta.Target.Method.Parameters.Select((p, i) => new { Name = p.Name, Value = meta.Target.Method.GetParameter(i) }).ToArray();

                        // Log parameter names instead of values for now
                        var paramNames = string.Join(", ", args.Select(a => a.Name));

                        // Log method entry with parameter names
                        ((ILogger)logger).Log(
                            entryLogLevel,
                            "Entering {MethodName} with parameters: {Parameters}",
                            methodName,
                            paramNames);
                    }
                    catch (Exception ex)
                    {
                        ((ILogger)logger).Log(
                            entryLogLevel,
                            "Entering {MethodName} with parameters (serialization failed): {Error}",
                            methodName,
                            ex.Message);
                    }
                }
                else
                {
                    ((ILogger)logger).Log(entryLogLevel, "Entering {MethodName}", methodName);
                }
            }

            try
            {
                // Execute the actual method
                var result = meta.Proceed();

                // Log successful execution with return value if enabled
                if (LogReturnValue && logger.IsEnabled(successLogLevel))
                {
                    if (result != null)
                    {
                        try
                        {
                            // For complex return types, just log the type to avoid serialization issues
                            var typeName = result.GetType().Name;
                            ((ILogger)logger).Log(
                                successLogLevel,
                                "Successfully executed {MethodName}, returned object of type: {ReturnType}",
                                methodName,
                                typeName);
                        }
                        catch
                        {
                            ((ILogger)logger).Log(
                                successLogLevel,
                                "Successfully executed {MethodName}, unable to log return value",
                                methodName);
                        }
                    }
                    else
                    {
                        ((ILogger)logger).Log(
                            successLogLevel,
                            "Successfully executed {MethodName}, returned null",
                            methodName);
                    }
                }
                else
                {
                    ((ILogger)logger).Log(successLogLevel, "Successfully executed {MethodName}", methodName);
                }

                return result;
            }
            catch (Exception ex)
            {
                // Log exception and rethrow
                ((ILogger)logger).Log(
                    exceptionLogLevel,
                    ex,
                    "Exception in {MethodName}: {ExceptionMessage}",
                    methodName,
                    ex.Message);
                throw;
            }
        }
    }
}
