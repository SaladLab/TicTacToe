using System;
using System.Linq;
using System.Reflection;
using Akka.Interfaced;
using Newtonsoft.Json;
using Common.Logging;
using System.Collections.Generic;

namespace GameServer
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class LogAttribute : Attribute, IFilterPerInstanceMethodFactory
    {
        private FieldInfo _loggerFieldInfo;
        private string _methodShortName;

        void IFilterPerInstanceMethodFactory.Setup(Type actorType, MethodInfo method)
        {
            _loggerFieldInfo = actorType.GetField("_logger", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _methodShortName = method.Name.Split('.').Last();
        }

        IFilter IFilterPerInstanceMethodFactory.CreateInstance(object actor)
        {
            var logger = actor != null ? (ILog)_loggerFieldInfo.GetValue(actor) : null;
            return new LogFilter(logger, _methodShortName);
        }
    }

    public class LogFilter : IPreHandleFilter, IPostHandleFilter
    {
        private static readonly JsonSerializerSettings _settings;
        private ILog _logger;
        private string _methodShortName;

        static LogFilter()
        {
            _settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new SurrogateSimpleConverter() },
            };
        }

        public LogFilter(ILog logger, string methodShortName)
        {
            _logger = logger;
            _methodShortName = methodShortName;
        }

        int IFilter.Order => 0;


        void IPreHandleFilter.OnPreHandle(PreHandleFilterContext context)
        {
            var invokeJson = JsonConvert.SerializeObject(context.Request.InvokePayload, _settings);
            _logger.TraceFormat("#{0} -> {1} {2}",
                                context.Request.RequestId, _methodShortName, invokeJson);
        }

        void IPostHandleFilter.OnPostHandle(PostHandleFilterContext context)
        {
            if (context.Response.Exception != null)
            {
                _logger.TraceFormat("#{0} <- {1} Exception: {2}",
                                    context.Request.RequestId, _methodShortName, context.Response.Exception);
            }
            else if (context.Response.ReturnPayload != null)
            {
                var returnJson = JsonConvert.SerializeObject(context.Response.ReturnPayload, _settings);
                _logger.TraceFormat("#{0} <- {1} {2}",
                                    context.Request.RequestId, _methodShortName, returnJson);
            }
            else
            {
                _logger.TraceFormat("#{0} <- {1} <void>",
                                    context.Request.RequestId, _methodShortName);
            }
        }
    }

    // This is quite simple class for dealing with serializing ISurrogated instances.
    // Without this converter, json default serailizer easily gets lost in inspecting IActorRef object.
    // Because sole purpose is just writing log, it uses ToString instead of ISurrogated context.
    internal class SurrogateSimpleConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (typeof(Akka.Util.ISurrogated).IsAssignableFrom(objectType))
                return true;

            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
