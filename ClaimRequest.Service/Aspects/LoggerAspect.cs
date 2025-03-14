using Metalama.Framework.Aspects;
using Microsoft.Extensions.Logging;
using System;

namespace ClaimRequest.BLL.Aspects
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class LoggerAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            string methodName = meta.Target.Method.Name;
            string typeName = meta.Target.Type.Name;
            string fullMethodName = $"{typeName}.{methodName}";

            try
            {
                // Log method entry with minimal information
                meta.This._logger.LogDebug("Entering {MethodName}", fullMethodName);

                // Execute the original method
                var startTime = DateTime.UtcNow;
                var result = meta.Proceed();
                var executionTime = DateTime.UtcNow - startTime;

                // Log method completion
                meta.This._logger.LogInformation(
                    "Successfully executed {MethodName} in {ExecutionTime}ms",
                    fullMethodName,
                    executionTime.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                // Log exceptions
                meta.This._logger.LogError(
                    ex,
                    "Exception in {MethodName}: {Message}",
                    fullMethodName,
                    ex.Message);

                throw;
            }
        }
    }
}