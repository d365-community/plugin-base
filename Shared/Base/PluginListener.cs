using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace D365.Community.Shared.Base
{
    /// <summary>
    /// This plugin listener class wraps execute and provides generic methods/functions, which are implemented most often in each IPlugin implementation!
    /// </summary>
    public abstract class PluginListener : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var stopwatch = Stopwatch.StartNew();
            var traces = new List<string>();
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var logging = new LoggingService(serviceProvider, context, GetModeName(context), GetStageName(context));
            using (logging.BeginScope)
            {
                try
                {
                    Execute(serviceProvider, context, ref traces);
                    if (traces.Count > 0)
                    {
                        logging.LogFullContext(LoggingService.Level.Information, context);
                    }
                    foreach (var trace in traces)
                    {
                        logging.LogInformation(trace);
                    }
                }
                catch (InvalidPluginExecutionException ipe)
                {
                    var message = ipe.GetBaseException().Message ?? "--no error message--";
                    if (ipe.Status == OperationStatus.Succeeded)
                    {
                        logging.LogFullContext(LoggingService.Level.Warning, context);
                        logging.LogWarning("Message:");
                        logging.LogWarning(message);
                    }
                    else
                    {
                        logging.LogFullContext(LoggingService.Level.Error, context);
                        logging.LogError("Message:");
                        logging.LogError(message);
                    }
                    logging.LogInformation("Traces:");
                    foreach (var trace in traces)
                    {
                        logging.LogInformation(trace);
                    }
                    if (ipe.Status != OperationStatus.Succeeded)
                    {
                        logging.DumpErrorDetails(LoggingService.Level.Error, ipe);
                    }
                    throw;
                }
                catch (Exception e)
                {
                    var message = e.GetBaseException().Message ?? "--no error message--";
                    logging.LogFullContext(LoggingService.Level.Fatal, context);
                    logging.LogFatal("Message:");
                    logging.LogFatal(message);
                    logging.DumpErrorDetails(LoggingService.Level.Fatal, e);
                    logging.LogInformation("Traces:");
                    foreach (var trace in traces)
                    {
                        logging.LogInformation(trace);
                    }
                    throw new InvalidPluginExecutionException(message, e);
                }
                finally
                {
                    stopwatch.Stop();
                    logging.Metric("ExecutionTime", stopwatch.ElapsedMilliseconds);
                }
            }
        }

        protected abstract void Execute(IServiceProvider serviceProvider, IPluginExecutionContext context, ref List<string> traces);

        protected IOrganizationService GetServiceSecured(IServiceProvider serviceProvider, IPluginExecutionContext context)
        {
            return ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(context.UserId);
        }

        protected IOrganizationService GetServiceElevated(IServiceProvider serviceProvider)
        {
            return ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(null);
        }

        protected bool GetTarget<T>(IPluginExecutionContext context, out T entity) where T : Entity
        {
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity target)
            {
                entity = target.ToEntity<T>();
                return true;
            }
            entity = null;
            return false;
        }

        protected bool GetReference(IPluginExecutionContext context, out EntityReference entityReference)
        {
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is EntityReference target)
            {
                entityReference = target;
                return true;
            }
            entityReference = null;
            return false;
        }

        protected bool GetPostImage<T>(IPluginExecutionContext context, string image, out T entity) where T : Entity
        {
            if (context.PostEntityImages.Contains(image) && context.PostEntityImages[image] is Entity post)
            {
                entity = post.ToEntity<T>();
                return true;
            }
            entity = null;
            return false;
        }

        protected bool GetPostImage<T>(IPluginExecutionContext context, out T entity) where T : Entity
        {
            return GetPostImage(context, "PostImage", out entity);
        }

        protected bool GetPreImage<T>(IPluginExecutionContext context, string image, out T entity) where T : Entity
        {
            if (context.PreEntityImages.Contains(image) && context.PreEntityImages[image] is Entity pre)
            {
                entity = pre.ToEntity<T>();
                return true;
            }
            entity = null;
            return false;
        }

        protected bool GetPreImage<T>(IPluginExecutionContext context, out T entity) where T : Entity
        {
            return GetPreImage(context, "PreImage", out entity);
        }

        protected string GetMessageName(IPluginExecutionContext context)
        {
            return context.MessageName.ToLowerInvariant();
        }

        protected string GetStageName(IPluginExecutionContext context)
        {
            //10 (pre-validation), 20 (pre-operation), 40 (post-operation)
            switch (context.Stage)
            {
                case 10:
                    return "pre-validation";
                case 20:
                    return "pre-operation";
                case 30:
                    return "main-operation";
                case 40:
                case 50:
                    return "post-operation";
                default:
                    return $"{context.Stage}";
            }
        }

        protected string GetModeName(IPluginExecutionContext context)
        {
            switch (context.Mode)
            {
                case 0:
                    return "synchronous";
                case 1:
                    return "asynchronous";
                default:
                    return $"{context.Mode}";
            }
        }

        protected string GetFullOperation(IPluginExecutionContext context)
        {
            return $"{GetMessageName(context)}.{GetStageName(context)}.{GetModeName(context)}";
        }

        protected T GetInputParameter<T>(IPluginExecutionContext context, string parameter)
        {
            return context.InputParameters.Contains(parameter) ? (T)context.InputParameters[parameter] : default;
        }

        protected bool GetInputParameter<T>(IPluginExecutionContext context, string parameter, out T value)
        {
            value = context.InputParameters.Contains(parameter) ? (T)context.InputParameters[parameter] : default;
            return context.InputParameters.Contains(parameter);
        }

        protected void SetOutputParameter<T>(IPluginExecutionContext context, string parameter, T value)
        {
            context.OutputParameters[parameter] = value;
        }

        private class LoggingService
        {
            private readonly IPluginExecutionContext _context;
            private readonly string _mode;
            private readonly string _stage;
            private readonly ITracingService _tracer;
            private readonly ILogger _telemetry;

            internal enum Level
            {
                Information,
                Warning,
                Error,
                Fatal
            }

            internal LoggingService(IServiceProvider serviceProvider, IPluginExecutionContext context, string mode, string stage)
            {
                _context = context;
                _mode = mode;
                _stage = stage;
                _tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
                _telemetry = (ILogger)serviceProvider.GetService(typeof(ILogger));
            }

            internal IDisposable BeginScope => _telemetry.BeginScope(new Dictionary<string, object>
            {
                ["system"] = "d365.plugin",
                ["correlationId"] = $"{_context.CorrelationId:D}",
                ["message"] = _context.MessageName,
                ["mode"] = _mode,
                ["stage"] = _stage,
                ["depth"] = $"{_context.Depth}",
                ["caller"] = $"{_context.InitiatingUserId:D}",
                ["user"] = $"{_context.UserId:D}",
                ["businessunit"] = $"{_context.BusinessUnitId:D}",
                ["entityname"] = _context.PrimaryEntityName,
                ["entityid"] = $"{_context.PrimaryEntityId:D}"
            });

            internal void LogInformation(string message, params object[] args)
            {
                Log(Level.Information, message, args);
            }

            internal void LogWarning(string message, params object[] args)
            {
                Log(Level.Warning, message, args);
            }

            internal void LogError(string message, params object[] args)
            {
                Log(Level.Error, message, args);
            }

            internal void LogFatal(string message, params object[] args)
            {
                Log(Level.Fatal, message, args);
            }

            private void Log(Level level, string message, params object[] args)
            {
                switch (level)
                {
                    case Level.Warning:
                        _tracer.Trace($"WARNING - {message}", args);
                        _telemetry.LogWarning(message, args);
                        break;
                    case Level.Error:
                        _tracer.Trace($"ERROR - {message}", args);
                        _telemetry.LogError(message, args);
                        break;
                    case Level.Fatal:
                        _tracer.Trace($"FATAL - {message}", args);
                        _telemetry.LogCritical(message, args);
                        break;
                    case Level.Information:
                    default:
                        _tracer.Trace(message, args);
                        _telemetry.LogInformation(message, args);
                        break;
                }
            }

            internal void Metric(string metric, long value)
            {
                _telemetry.LogMetric(metric, value);
            }

            internal void LogFullContext(Level lvl, IPluginExecutionContext context)
            {
                if (GetParentContext(context, out var parent) && (context.PrimaryEntityName != parent.PrimaryEntityName || context.PrimaryEntityId != parent.PrimaryEntityId))
                {
                    if (GetRootContext(context, out var root) && parent != root)
                    {
                        Log(lvl, $"RootContext: {root.PrimaryEntityName}({root.PrimaryEntityId:D}) on message {root.MessageName}");
                    }
                    Log(lvl, $"ParentContext: {parent.PrimaryEntityName}({parent.PrimaryEntityId:D}) on message {parent.MessageName}");
                }
                if (context.PrimaryEntityId != Guid.Empty) Log(lvl, $"Record-ID: {context.PrimaryEntityId:D}");
            }

            internal void DumpErrorDetails(Level lvl, Exception e)
            {
                Log(lvl, "SharedVariables:");
                foreach (var sharedVariable in _context.SharedVariables)
                {
                    Log(lvl, $" - {sharedVariable.Key} -> {sharedVariable.Value}");
                }
                Log(lvl, "InputParameters:");
                foreach (var inputParameter in _context.InputParameters)
                {
                    Log(lvl, $" - {inputParameter.Key} -> {inputParameter.Value}");
                }
                Log(lvl, "PreEntityImages:");
                foreach (var preEntityImage in _context.PreEntityImages)
                {
                    Log(lvl, $" - {preEntityImage.Key} -> {preEntityImage.Value}");
                }
                Log(lvl, "PostEntityImages:");
                foreach (var postEntityImage in _context.PostEntityImages)
                {
                    Log(lvl, $" - {postEntityImage.Key} -> {postEntityImage.Value}");
                }
                Log(lvl, "StackTrace:");
                Log(lvl, e.GetBaseException().StackTrace);
            }

            private static bool GetParentContext(IPluginExecutionContext context, out IPluginExecutionContext parent)
            {
                parent = context?.ParentContext;
                return parent != null;
            }

            private static bool GetRootContext(IPluginExecutionContext context, out IPluginExecutionContext root)
            {
                var inner = context;
                do
                {
                    root = inner?.ParentContext;
                    inner = root;
                } while (root != null);
                return root != null;
            }
        }
    }
}