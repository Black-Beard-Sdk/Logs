using log4net.Config;
using log4net.Core;
using log4net.Repository;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bb.Logs
{

    /// <summary>
    /// Implementation of <see cref="System.Diagnostics.TraceListener" /> with log4net
    /// </summary>
    public class Log4NetTraceListener : System.Diagnostics.TraceListener
    {

        /// <summary>
        /// Initialize log with log4net
        /// </summary>
        /// <param name="name"></param>
        /// <param name="log4NetconfigPath"></param>
        public static void Initialize(string name, string log4NetconfigPath = "Log4Net.config")
        {
            Log4NetTraceListener log = new Log4NetTraceListener(name, log4NetconfigPath);
            System.Diagnostics.Trace.Listeners.Add(log);
        }

        private Log4NetTraceListener(string name, string log4NetconfigPath)
        {

            Name = name;
            Assembly assembly = typeof(Log4NetTraceListener).Assembly;

            _logger = LoggerManager.GetLogger(assembly, name);
            _repository = _logger.Repository;
            XmlConfigurator.Configure(_repository, new FileInfo(log4NetconfigPath));

            LogInternal(Level.Info, "Log initialized", new List<KeyValuePair<string, object>>());

        }

        /// <summary>
        /// Add a new track level on the log
        /// </summary>
        /// <param name="value"></param>
        /// <param name="name"></param>
        /// <param name="displayName"></param>
        public static void AddLevel(int value, string name, string displayName)
        {
            LogLevel.AddLevel(value, name, displayName);
        }

        public override void Fail(string message)
        {
            base.Fail(message);
        }

        public override void Fail(string message, string detailMessage)
        {
            base.Fail(message, detailMessage);
        }

        public override void Write(string message)
        {
            WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            LogInternal(Level.Info, message, new List<KeyValuePair<string, object>>());
        }

        public override void Write(object o)
        {
            WriteLine(o);
        }

        public override void Write(object o, string category)
        {
            if (o is Exception e)
            {
                LogInternal(category.ConvertLevel(), e.Message, e, new List<KeyValuePair<string, object>>());
            }
            else
            {
                var p = GetParameters(o, out string msg);
                LogInternal(category.ConvertLevel(), msg, p);
            }
        }

        public override void Write(string message, string category)
        {
            WriteLine(message, category);
        }

        public override void WriteLine(object o, string category)
        {
            if (o is Exception e)
            {
                LogInternal(category.ConvertLevel(), e.Message, e, new List<KeyValuePair<string, object>>());
            }
            else
            {
                var p = GetParameters(o, out string msg);
                LogInternal(category.ConvertLevel(), msg, p);
            }
        }

        public override void WriteLine(string message, string category)
        {
            LogInternal(category.ConvertLevel(), message, new List<KeyValuePair<string, object>>());
        }

        public override void WriteLine(object o)
        {
            if (o is Exception e)
            {
                LogInternal(Level.Info, e.Message, e, new List<KeyValuePair<string, object>>());
            }
            else
            {
                var p = GetParameters(o, out string msg);
                LogInternal(Level.Info, msg, p);
            }

        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public List<KeyValuePair<string, object>> GetParameters(object parameters, out string message)
        {

            var dic = parameters.GetDictionnaryProperties(false);

            List<KeyValuePair<string, object>> _parameters = new List<KeyValuePair<string, object>>();

            message = string.Empty;

            foreach (var item in dic)
            {

                switch (item.Key.ToLower())
                {

                    case "text":
                    case "txt":
                    case "message":
                    case "msg":
                        message = item.Value.ToString();
                        break;

                    default:
                        _parameters.Add(new KeyValuePair<string, object>(item.Key, item.Value));
                        break;

                }

            }

            return _parameters;

        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogInternal(Level logLevel, string message, Exception ex, List<KeyValuePair<string, object>> dicParameters)
        {

            if (logLevel != Level.Off)
            {

                log4net.Core.LoggingEvent logEntry = CreateLogEntry(logLevel, message, ex, dicParameters);

                try
                {
                    _logger.Log(logEntry);
                }
                catch (Exception ex1)
                {
                    Console.WriteLine("Log fail. " + ex1.Message);
                }

            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogInternal(Level logLevel, string message, List<KeyValuePair<string, object>> dicParameters)
        {

            if (logLevel != Level.Off)
            {

                log4net.Core.LoggingEvent logEntry = CreateLogEntry(logLevel, message, null, dicParameters);

                try
                {
                    _logger.Log(logEntry);
                }
                catch (Exception ex1)
                {
                    Console.WriteLine("Log fail. " + ex1.Message);
                }

            }

        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private log4net.Core.LoggingEvent CreateLogEntry(log4net.Core.Level level, string message, Exception ex, List<KeyValuePair<string, object>> customInfos)
        {

            if (ex != null)
            {
                SerializeException(ex, customInfos);
            }

            string m = !string.IsNullOrEmpty(message)
                ? message
                : ex != null && ex.Message != null
                    ? ex.Message
                    : string.Empty;

            log4net.Core.LoggingEvent result;
            if (ex == null)
            {
                result = Generate(level, m);
            }
            else
            {
                result = Generate(level, ex, m);
            }

            foreach (var item in customInfos)
            {
                if (!string.IsNullOrEmpty(item.Key))
                {
                    result.Properties[item.Key] = item.Value;
                }
            }

            return result;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LoggingEvent Generate(log4net.Core.Level level, Exception ex, string m)
        {
            log4net.Core.LoggingEvent result;
            var reflectedType = ex.TargetSite != null ? ex.TargetSite.ReflectedType : typeof(object);
            result = new log4net.Core.LoggingEvent(reflectedType, _repository, Name, level, m, ex);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LoggingEvent Generate(log4net.Core.Level level, string m)
        {
            StackFrame stackFrame = null;
            System.Reflection.MethodBase method = null;

            var stackFrames = new StackTrace().GetFrames();

            for (int indexFrame = 0; indexFrame < stackFrames.Length; ++indexFrame)
            {
                stackFrame = stackFrames[indexFrame];
                method = stackFrame.GetMethod();
                if (method.DeclaringType != typeof(Log4NetTraceListener))
                {
                    break;
                }
            }

            var result = new log4net.Core.LoggingEvent(method.ReflectedType, _repository, new LoggingEventData()
            {
                Level = level,
                Message = m,
                LoggerName = Name,
                LocationInfo = new LocationInfo(GetMethodName(method),
                                                method.ToString(),
                                                stackFrame.GetFileName(),
                                                stackFrame.GetFileLineNumber().ToString())
            }, FixFlags.All);
            return result;
        }


        private static string GetMethodName(System.Reflection.MethodBase method)
        {
            var type = method.ReflectedType;
            if (type != null)
            {
                return type.Name;
            }
            else
            {
                return method.ToString();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SerializeException(Exception ex, List<KeyValuePair<string, object>> dicParameters)
        {

            if (!string.IsNullOrEmpty(ex.Source))
            {
                dicParameters.Add(new KeyValuePair<string, object>("Source", ex.Source));
            }

            if (ex.TargetSite != null)
            {
                dicParameters.Add(new KeyValuePair<string, object>("Method", ex.TargetSite.Name));
            }

            if (ex.InnerException != null)
            {
                dicParameters.Add(new KeyValuePair<string, object>("InnerException", ex.InnerException.ToString()));
            }
        }

        private readonly ILogger _logger;
        private readonly ILoggerRepository _repository;

    }

    internal static class LogLevel
    {

        static LogLevel()
        {
            _level = new Dictionary<string, Level>();

        }

        internal static void AddLevel(int value, string name, string displayName)
        {
            _level.Add(name, new Level(value, name, displayName));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Level ConvertLevel(this string category)
        {

            string cat = category.ToLower();

            switch (cat)
            {

                case "off":
                    return Level.Off;           // 2 147 483 647

                case "log4net_debug":
                    return Level.Log4Net_Debug; // 120 000

                case "emergency":
                    return Level.Emergency;     // 120 000

                case "fatal":
                    return Level.Fatal;         // 110 000

                case "alert":
                    return Level.Alert;         // 100 000

                case "critical":
                    return Level.Critical;      // 90 000

                case "severe":
                    return Level.Severe;        // 80 000

                case "error":
                    return Level.Error;         // 70 000

                case "warn":
                    return Level.Warn;          // 60 000

                case "notice":
                    return Level.Notice;        // 50 000

                case "info":
                    return Level.Info;          // 40 000

                case "debug":
                    return Level.Debug;         // 30 000

                case "fine":
                    return Level.Fine;          // 30 000

                case "finer":
                    return Level.Finer;         // 20 000

                case "trace":
                    return Level.Trace;         // 20 000

                case "finest":
                    return Level.Finest;        // 10 000

                case "verbose":
                    return Level.Verbose;       // 10 000

                case "all":
                    return Level.All;           // -2 147 483 648

                default:
                    if (!_level.TryGetValue(cat, out Level level))
                        lock(_lock)
                            if (!_level.TryGetValue(cat, out level))
                                _level.Add(cat, (level = new Level(Level.Verbose.Value - 1, cat)));

                    return level;
                    
            }

        }

        private static Dictionary<string, Level> _level;
        private static readonly object _lock = new object();

    }

    internal static class LoggerExtension
    {

        /// <summary>
        /// format the serialization of the specified object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">object sour to serialized</param>
        /// <param name="format">output format</param>
        /// <returns>string of the format with the source propertie's serialized</returns>
        public static Dictionary<string, object> GetDictionnaryProperties(this object source, bool ignoreCase = true)
        {
            System.Diagnostics.Contracts.Contract.Requires(!object.Equals(source, null), "null reference exception 'source'");
            return GetPropertiesMethod(source.GetType(), ignoreCase)(source);
        }

        #region Compile object serialization

        /// <summary>
        /// Get compiled method or create
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private static Func<object, Dictionary<string, object>> GetPropertiesMethod(Type type, bool ignoreCase)
        {

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!_funcs.TryGetValue(type, out Func<object, Dictionary<string, object>> result))
            {
                lock (_lock)
                {
                    if (!_funcs.TryGetValue(type, out result))
                    {
                        _funcs.Add(type, result = CompileObject(type, ignoreCase));
                    }
                }
            }

            return result;

        }

        /// <summary>
        /// Compile method for serialize all properties of the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeSource"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private static Func<object, Dictionary<string, object>> CompileObject(Type typeSource, bool ignoreCase)
        {

            Type containerType = typeof(Dictionary<string, object>);
            Type stringType = typeof(string);
            var _properties = typeSource.GetAllProperties().Where(c => c.CanRead);
            var properties = new HashSet<string>();
            var m = containerType.GetNamedMethod("Add", typeof(string), typeof(object));

            // variables
            var parameterIn = Expression.Parameter(typeof(object), "argIn");
            var parameter = Expression.Variable(typeSource, "arg1");
            var dic = Expression.Variable(containerType, "dic");

            var lst = new List<Expression>()
            {
                // var dic = new Dictionary<string, object>();
                dic.CreateObject(),
                parameter.SettedBy(parameterIn.As(typeSource)),

            };

            foreach (PropertyInfo item in _properties)
            {

                var n = item.Name;

                if (ignoreCase)
                {
                    n = n.ToLower();
                }

                if (properties.Add(item.Name))
                {
                    var p = item.PropertyType;
                    var m1 = parameter.Member(item);

                    if (p == typeof(object))
                    {
                        lst.Add(dic.Invoke(m, Expression.Constant(n), m1));
                    }
                    else
                    {
                        lst.Add(dic.Invoke(m, Expression.Constant(n), m1.As(typeof(object))));
                    }
                }
            }
            // return builder.ToString();
            lst.Add(dic);

            // Create func
            BlockExpression block = Expression.Block(containerType, new ParameterExpression[] { dic, parameter }, lst);
            var lbd = Expression.Lambda<Func<object, Dictionary<string, object>>>(block, parameterIn);

            return lbd.Compile();

        }

        private static readonly object _lock = new object();

        #endregion Compile object serialization

        #region Expressions

        public static BinaryExpression CreateObject(this Expression self)
        {
            return self.SettedBy(Expression.New(self.Type));
        }

        public static BinaryExpression SettedBy(this Expression self, Expression right)
        {
            return Expression.Assign(self, right);
        }

        public static UnaryExpression As(this Expression self, Type type)
        {
            return Expression.ConvertChecked(self, type);
        }

        public static MemberExpression Member(this Expression self, PropertyInfo property)
        {
            return Expression.Property(self, property);
        }

        public static MethodCallExpression Invoke(this Expression self, MethodInfo method, params Expression[] args)
        {

            if (args.Length == 0)
            {
                return Expression.Call(self, method);
            }
            else
            {
                return Expression.Call(self, method, args);
            }
        }

        public static MethodInfo GetNamedMethod(this Type componentType, string name, params Type[] args)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("message", nameof(name));
            }

            var methods = GetAllMethods(componentType)
                //.Where(c => c.IsPublic)
                .ToList();

            foreach (var item in methods.Where(c => c.Name == name))
            {
                var args2 = item.GetParameters();
                if (args.Length == args2.Length)
                {

                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i] != args2[i].ParameterType)
                        {
                            continue;
                        }
                    }

                    return item;

                }
            }

            return null;

        }

        public static IEnumerable<MethodInfo> GetAllMethods(this Type componentType)
        {

            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var type = componentType;

            while (type != null && type != typeof(object))
            {

                foreach (var item in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    yield return item;
                }

                type = type.BaseType;

            }
        }

        public static IEnumerable<PropertyInfo> GetAllProperties(this Type componentType)
        {

            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var type = componentType;

            while (type != null && type != typeof(object))
            {

                foreach (var item in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    yield return item;
                }

                type = type.BaseType;

            }
        }

        #endregion Expressions

        #region private

        private static Dictionary<Type, Func<object, Dictionary<string, object>>> _funcs = new Dictionary<Type, Func<object, Dictionary<string, object>>>();

        #endregion private

    }

}
