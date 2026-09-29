using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace YANF.Tests
{
    /// <summary>
    /// Reflection access to private members, for the few checks that need them. Internal members are used directly
    /// (the library grants InternalsVisibleTo("YANF.Tests")); a renamed private member fails the test with its name.
    /// </summary>
    internal static class Priv
    {
        #region Fields
        private const BindingFlags ANY = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        private const BindingFlags ANY_STATIC = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
        #endregion

        #region Methods
        /// <summary>
        /// Reads an instance field declared on the object's type or one of its base types.
        /// </summary>
        public static T Field<T>(object obj, string name)
        {
            for (var t = obj.GetType(); t != null; t = t.BaseType)
            {
                var f = t.GetField(name, ANY);
                if (f != null)
                {
                    return (T)f.GetValue(obj);
                }
            }
            throw new MissingFieldException(obj.GetType().FullName, name);
        }

        /// <summary>
        /// Reads a non-public instance property (for example a protected override) declared on the object's type or a base type.
        /// </summary>
        public static T Property<T>(object obj, string name)
        {
            for (var t = obj.GetType(); t != null; t = t.BaseType)
            {
                var p = t.GetProperty(name, ANY);
                if (p != null)
                {
                    return (T)p.GetValue(obj);
                }
            }
            throw new MissingMemberException(obj.GetType().FullName, name);
        }

        /// <summary>
        /// Calls an instance method (matched by name and argument count) and rethrows what it throws.
        /// </summary>
        public static object Call(object obj, string name, params object[] args)
        {
            for (var t = obj.GetType(); t != null; t = t.BaseType)
            {
                var m = t.GetMethods(ANY).FirstOrDefault(x => x.Name == name && x.GetParameters().Length == args.Length);
                if (m != null)
                {
                    return Invoke(m, obj, args);
                }
            }
            throw new MissingMethodException(obj.GetType().FullName, name);
        }

        /// <summary>
        /// Calls a static method (matched by name and argument count) and rethrows what it throws.
        /// </summary>
        public static object CallStatic(Type type, string name, params object[] args)
        {
            var m = type.GetMethods(ANY_STATIC).FirstOrDefault(x => x.Name == name && x.GetParameters().Length == args.Length);
            return m == null ? throw new MissingMethodException(type.FullName, name) : Invoke(m, null, args);
        }

        /// <summary>
        /// Reads a static field of a type.
        /// </summary>
        public static T StaticField<T>(Type type, string name)
        {
            var f = type.GetField(name, ANY_STATIC);
            return f == null ? throw new MissingFieldException(type.FullName, name) : (T)f.GetValue(null);
        }

        private static object Invoke(MethodInfo m, object target, object[] args)
        {
            try
            {
                return m.Invoke(target, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
        #endregion
    }
}
