using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;
using YANF.Control;
using static System.ComponentModel.DesignerSerializationVisibility;

namespace YANF.Tests.Controls
{
    using Control = System.Windows.Forms.Control;

    /// <summary>
    /// Designer metadata of every public control of the YANF.Control namespace, found by reflection (a new control is covered
    /// without editing this file). The designer writes a property into Designer.cs only when its value differs from its
    /// [DefaultValue]: a [DefaultValue] that differs from what the constructor gives silently drops a real value from the form
    /// (1.0.2 lost YANDdl.AutoCompleteMode None this way), and a property without one is written on every save, even at its default.
    /// </summary>
    public class DesignerMetadataTests
    {
        #region Fields
        private const string CONTROL_NAMESPACE = "YANF.Control";
        private static readonly Assembly LIBRARY = typeof(YANBtn).Assembly;

        // The categories of the properties and events that YANF declares (an override of a framework member may keep the framework's)
        private static readonly string[] YAN_CATEGORIES = { "YAN Appearance", "YAN Behavior", "YAN Data", "YAN Event" };

        // Designer-written properties declared by YANF that have no constant default, and why. The designer writes them through the
        // framework's ShouldSerialize method, that is whenever they are set on the control itself
        private static readonly Dictionary<string, string> NO_CONSTANT_DEFAULT = new()
        {
            ["Font"] = "the constructors build it from the name of the default font, which depends on the machine (YANPrg keeps the ambient font)"
        };

        // Framework properties whose inherited [DefaultValue] differs from the value that the constructor sets. The designer writes the
        // constructor value (it differs from the default) but never the framework default itself, which the constructor then replaces at
        // run time: a user who picks that value in the designer loses it. A new mismatch fails the test, and so does a listed one that
        // no longer occurs. 2.0 fixed the three of 1.x: YANBtn.FlatStyle and YANCirPic.SizeMode are redeclared with the constructor's
        // default, and YANBtn.FlatAppearance.BorderSize gets it from a type description provider of the button's FlatAppearance
        private static readonly Dictionary<string, string> KNOWN_INHERITED_MISMATCHES = new();

        // Inherited framework properties whose value in a new control depends on the process rather than on the control, and why
        private static readonly Dictionary<string, string> PROCESS_DEPENDENT = new()
        {
            ["UseCompatibleTextRendering"] = "follows Application.SetCompatibleTextRenderingDefault, which applications set to false in Main before "
                + "they create a window (the default, true, applies to the test process)"
        };
        #endregion

        #region Data
        /// <summary>
        /// Every public, non-abstract control type of the YANF.Control namespace.
        /// </summary>
        public static TheoryData<Type> ControlTypes
        {
            get
            {
                var data = new TheoryData<Type>();
                foreach (var type in GetControlTypes())
                {
                    data.Add(type);
                }
                return data;
            }
        }
        #endregion

        #region Tests
        // The reflection finds the controls (a wrong namespace or assembly would make every theory below pass without data)
        [Fact]
        public void ControlTypes_AreFound()
        {
            var names = GetControlTypes().Select(t => t.Name).ToList();
            foreach (var name in new[] { "YANBtn", "YANCirPic", "YANDdl", "YANDp", "YANGradPnl", "YANNb", "YANPrg", "YANRdo", "YANTg", "YANTxt" })
            {
                Assert.Contains(name, names);
            }
        }

        // Every [DefaultValue] of a property declared by YANF (overrides included) equals the value of a new control, compared the way
        // the designer compares it, so the designer writes nothing for a new control and never drops a value that the user picked
        [Theory]
        [MemberData(nameof(ControlTypes))]
        public void DefaultValue_MatchesTheConstructor(Type type) => Sta.Run(() =>
        {
            using var c = (Control)Activator.CreateInstance(type);
            var own = new HashSet<string>(OwnProperties(type).Select(p => p.Name));
            var failures = new List<string>();
            foreach (PropertyDescriptor p in TypeDescriptor.GetProperties(c))
            {
                if (own.Contains(p.Name) && p.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute dv)
                {
                    failures.AddRange(CheckDefault(c, p, dv, type.Name + "." + p.Name));
                }
            }
            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        });

        // Every property declared by YANF that the designer writes has a [DefaultValue] (or its own ShouldSerialize method): without one
        // the designer writes it on every save, shows it in bold and cannot reset it. A property without a constant default is listed in
        // NO_CONSTANT_DEFAULT with the reason
        [Theory]
        [MemberData(nameof(ControlTypes))]
        public void DesignerWrittenProperty_HasADefault(Type type) => Sta.Run(() =>
        {
            using var c = (Control)Activator.CreateInstance(type);
            var props = TypeDescriptor.GetProperties(c);
            var failures = new List<string>();
            foreach (var info in OwnProperties(type))
            {
                var p = props[info.Name];
                if (p == null || p.IsReadOnly || p.SerializationVisibility != Visible || p.Attributes[typeof(DefaultValueAttribute)] != null
                    || NO_CONSTANT_DEFAULT.ContainsKey(p.Name) || HasOwnShouldSerialize(type, p.Name))
                {
                    continue;
                }
                failures.Add($"{type.Name}.{p.Name} ({Show(p.GetValue(c))} in a new control) has no [DefaultValue]: the designer writes it on every save");
            }
            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        });

        // The inherited framework properties that the user can set in the property grid: their [DefaultValue] (also inside an expandable
        // property such as FlatAppearance) matches the value of a new control, except the known mismatches of KNOWN_INHERITED_MISMATCHES
        [Theory]
        [MemberData(nameof(ControlTypes))]
        public void InheritedDefaultValue_MatchesTheConstructor_OrIsKnown(Type type) => Sta.Run(() =>
        {
            using var c = (Control)Activator.CreateInstance(type);
            var own = new HashSet<string>(OwnProperties(type).Select(p => p.Name));
            var mismatches = new List<string>();
            foreach (PropertyDescriptor p in TypeDescriptor.GetProperties(c))
            {
                if (own.Contains(p.Name) || !p.IsBrowsable || p.SerializationVisibility == Hidden || PROCESS_DEPENDENT.ContainsKey(p.Name))
                {
                    continue;
                }
                var name = type.Name + "." + p.Name;
                if (p.SerializationVisibility == Content)
                {
                    // an expandable object is written property by property (FlatAppearance.BorderSize); a collection is written as items
                    var value = p.GetValue(c);
                    if (value != null && value is not IEnumerable)
                    {
                        foreach (PropertyDescriptor sub in TypeDescriptor.GetProperties(value))
                        {
                            if (sub.IsBrowsable && sub.SerializationVisibility == Visible && sub.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute subDv)
                            {
                                mismatches.AddRange(CheckDefault(value, sub, subDv, name + "." + sub.Name));
                            }
                        }
                    }
                }
                else if (p.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute dv)
                {
                    mismatches.AddRange(CheckDefault(c, p, dv, name));
                }
            }
            var names = mismatches.Select(m => m.Substring(0, m.IndexOf(':'))).ToList();
            var unknown = mismatches.Where(m => !KNOWN_INHERITED_MISMATCHES.ContainsKey(m.Substring(0, m.IndexOf(':')))).ToList();
            Assert.True(unknown.Count == 0, string.Join(Environment.NewLine, unknown));
            // a known mismatch that no longer occurs (YANF redeclares the property now, or gives it a default otherwise) is fixed: it must
            // leave the list (the tests above check the new declaration)
            foreach (var known in KNOWN_INHERITED_MISMATCHES.Keys.Where(k => k.StartsWith(type.Name + ".", StringComparison.Ordinal)))
            {
                Assert.False(own.Contains(known.Split('.')[1]), known + " is declared by YANF now: remove it from KNOWN_INHERITED_MISMATCHES");
                Assert.True(names.Contains(known), known + " matches the constructor now: remove it from KNOWN_INHERITED_MISMATCHES");
            }
        });

        // Every public property and event declared by YANF has a description and a category for the property grid. A member declared by
        // YANF uses one of the YAN categories; an override of a framework member may keep the framework's description and category, and
        // so may a redeclaration (new) of a framework property that only changes its designer default (2.0: YANBtn.FlatStyle,
        // YANCirPic.SizeMode). Two members of a control never share a description (1.0.2: YANPrg.ChannelHeight had the description of
        // SymbolBefore)
        [Theory]
        [MemberData(nameof(ControlTypes))]
        public void PropertyAndEvent_HaveADescriptionAndACategory(Type type)
        {
            var failures = new List<string>();
            var descriptions = new Dictionary<string, string>();
            foreach (var p in OwnProperties(type))
            {
                Check(p, IsOverride(p.GetGetMethod() ?? p.GetSetMethod()) || IsFrameworkRedeclaration(p));
            }
            foreach (var e in OwnEvents(type))
            {
                Check(e, IsOverride(e.GetAddMethod()));
            }
            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));

            void Check(MemberInfo member, bool isOverride)
            {
                var name = type.Name + "." + member.Name;
                // the most derived declaration comes first (an override may declare its own, the base one comes after it)
                var description = Attribute.GetCustomAttributes(member, typeof(DescriptionAttribute), true).OfType<DescriptionAttribute>().FirstOrDefault()?.Description;
                var category = Attribute.GetCustomAttributes(member, typeof(CategoryAttribute), true).OfType<CategoryAttribute>().FirstOrDefault()?.Category;
                if (string.IsNullOrWhiteSpace(description))
                {
                    failures.Add(name + " has no [Description]");
                }
                else if (descriptions.TryGetValue(description, out var other))
                {
                    failures.Add($"{name} has the description of {other}: \"{description}\"");
                }
                else
                {
                    descriptions.Add(description, name);
                }
                if (string.IsNullOrWhiteSpace(category))
                {
                    failures.Add(name + " has no [Category]");
                }
                else if (!isOverride && !YAN_CATEGORIES.Contains(category))
                {
                    failures.Add($"{name} is in the category \"{category}\" instead of one of: {string.Join(", ", YAN_CATEGORIES)}");
                }
            }
        }

        // The default property (selected when the property grid opens) and the default event (wired by a double click in the designer)
        // are members that the property grid shows
        [Theory]
        [MemberData(nameof(ControlTypes))]
        public void DefaultPropertyAndEvent_AreBrowsable(Type type)
        {
            var defaultProperty = TypeDescriptor.GetDefaultProperty(type);
            Assert.True(defaultProperty != null && defaultProperty.IsBrowsable, $"{type.Name}: the default property ({defaultProperty?.Name ?? "none"}) is not browsable");
            var defaultEvent = TypeDescriptor.GetDefaultEvent(type);
            Assert.True(defaultEvent != null && defaultEvent.IsBrowsable, $"{type.Name}: the default event ({defaultEvent?.Name ?? "none"}) is not browsable");
        }

        // The Toolbox shows the icon of the closest standard control (an image of System.Windows.Forms) instead of the generic gear
        [Theory]
        [MemberData(nameof(ControlTypes))]
        public void ToolboxBitmap_ShowsAStandardIcon(Type type)
        {
            var bitmap = (ToolboxBitmapAttribute)Attribute.GetCustomAttribute(type, typeof(ToolboxBitmapAttribute), false);
            Assert.True(bitmap != null, type.Name + " has no [ToolboxBitmap]: the Toolbox shows the generic gear icon");
            Assert.True(bitmap.GetImage(type) != null, type.Name + ": the [ToolboxBitmap] image is not found");
        }
        #endregion

        #region Methods
        // Get every public, non-abstract control type of the YANF.Control namespace
        private static IEnumerable<Type> GetControlTypes() => LIBRARY.GetExportedTypes()
            .Where(t => t.Namespace == CONTROL_NAMESPACE && t.IsClass && !t.IsAbstract && typeof(Control).IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal);

        // Get the type and its base types that YANF declares, the most derived first
        private static IEnumerable<Type> GetYanfTypes(Type type)
        {
            for (var t = type; t != null && t.Assembly == LIBRARY; t = t.BaseType)
            {
                yield return t;
            }
        }

        // Get the public instance properties that YANF declares, overrides and redeclarations (new) included (the most derived by name)
        private static IEnumerable<PropertyInfo> OwnProperties(Type type) => GetYanfTypes(type)
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(p => p.GetIndexParameters().Length == 0)
            .GroupBy(p => p.Name)
            .Select(g => g.First());

        // Get the public instance events that YANF declares, overrides and redeclarations (new) included (the most derived by name)
        private static IEnumerable<EventInfo> OwnEvents(Type type) => GetYanfTypes(type)
            .SelectMany(t => t.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .GroupBy(e => e.Name)
            .Select(g => g.First());

        // Check whether an accessor overrides a framework member (a redeclaration with new does not)
        private static bool IsOverride(MethodInfo accessor) => accessor.GetBaseDefinition().DeclaringType != accessor.DeclaringType;

        // Check whether a property declared by YANF with new hides a public property of the framework base class (same name and type)
        private static bool IsFrameworkRedeclaration(PropertyInfo p)
        {
            var framework = p.DeclaringType;
            while (framework != null && framework.Assembly == LIBRARY)
            {
                framework = framework.BaseType;
            }
            return framework != null && framework.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(b => b.Name == p.Name && b.PropertyType == p.PropertyType);
        }

        // Check whether YANF declares a ShouldSerialize method for the property (the framework's own ones, such as
        // Control.ShouldSerializeBackColor, write any value set in the constructor)
        private static bool HasOwnShouldSerialize(Type type, string name) => GetYanfTypes(type).Any(t => t.GetMethod("ShouldSerialize" + name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null) != null);

        // Compare a [DefaultValue] with the value of a new control the way the designer does: object.Equals, after the attribute has
        // converted its text with the converter of the given type (DefaultValue(typeof(Color), "White") is the known color White, which
        // is not equal to Color.FromArgb(255, 255, 255)). A value of another type (DefaultValue(0) on a float, a decimal or an enum)
        // never matches: the designer then writes the property on every save and Reset fails
        private static IEnumerable<string> CheckDefault(object component, PropertyDescriptor p, DefaultValueAttribute dv, string name)
        {
            object value;
            try
            {
                value = p.GetValue(component);
            }
            catch (Exception ex)
            {
                return new[] { $"{name}: reading it throws {ex.GetType().Name}: {ex.Message}" };
            }
            var defaultValue = dv.Value;
            if (defaultValue != null && !p.PropertyType.IsInstanceOfType(defaultValue))
            {
                return new[] { $"{name}: [DefaultValue] is a {defaultValue.GetType().Name} ({Show(defaultValue)}) but the property is a {p.PropertyType.Name}" };
            }
            if (!Equals(defaultValue, value))
            {
                return new[] { $"{name}: [DefaultValue({Show(defaultValue)})] but a new control has {Show(value)}" };
            }
            if (p.ShouldSerializeValue(component))
            {
                return new[] { $"{name}: the designer writes it at its [DefaultValue] ({Show(value)})" };
            }
            return Array.Empty<string>();
        }

        // Show a value in a failure message (a color by name, or by ARGB when it is not a named color)
        private static string Show(object value) => value switch
        {
            null => "null",
            string s => "\"" + s + "\"",
            Color color when color.IsNamedColor => "Color." + color.Name,
            Color color => $"Color.FromArgb({color.A}, {color.R}, {color.G}, {color.B})",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture) + " (" + value.GetType().Name + ")",
            _ => value.ToString()
        };
        #endregion
    }
}
