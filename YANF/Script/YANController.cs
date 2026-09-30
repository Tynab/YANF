using System;
using System.Collections.Generic;
using System.Linq;
using YANF.Control;
using static System.IO.Directory;
using static System.IO.Path;

namespace YANF.Script
{
    public static class YANController
    {
        /// <summary>
        /// Get tất cả control con theo loại.
        /// </summary>
        /// <param name="ctrl">Control cha.</param>
        /// <param name="type">Loại control cần get.</param>
        /// <returns>Control list.</returns>
        public static IEnumerable<System.Windows.Forms.Control> GetAllObjs(this System.Windows.Forms.Control ctrl, Type type)
        {
            var ctrls = ctrl.Controls.Cast<System.Windows.Forms.Control>();
            return ctrls.SelectMany(obj => obj.GetAllObjs(type)).Concat(ctrls).Where(c => c.GetType() == type);
        }

        /// <summary>
        /// Gets every descendant control of type <typeparamref name="T"/>, including controls of types derived from it
        /// (for example YANGradPnl for <see cref="System.Windows.Forms.Panel"/>).
        /// </summary>
        /// <typeparam name="T">Control type to look for.</typeparam>
        /// <param name="ctrl">Parent control (usually a form); the control itself is not included.</param>
        /// <returns>The matching descendants, depth-first in pre-order: each control comes before its own children, and siblings
        /// follow the order of their parent's Controls collection. The tree is walked lazily as the result is enumerated.</returns>
        /// <remarks>
        /// Unlike <see cref="GetAllObjs(System.Windows.Forms.Control, Type)"/>, which matches the exact type only, this matches with
        /// <c>is T</c>. It also walks into the inner controls of composite controls such as YANDdl; use the
        /// <see cref="GetAllObjs{T}(System.Windows.Forms.Control, Func{System.Windows.Forms.Control, bool})"/> overload to skip them.
        /// The walk uses an explicit stack, so deeply nested layouts cannot overflow the call stack.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="ctrl"/> is null.</exception>
        public static IEnumerable<T> GetAllObjs<T>(this System.Windows.Forms.Control ctrl) where T : System.Windows.Forms.Control => ctrl.GetAllObjs<T>(null);

        /// <summary>
        /// Gets every descendant control of type <typeparamref name="T"/> (or derived from it), walking only into the children of the
        /// controls accepted by <paramref name="descendInto"/>.
        /// </summary>
        /// <typeparam name="T">Control type to look for.</typeparam>
        /// <param name="ctrl">Parent control (usually a form); the control itself is not included and its children are always visited.</param>
        /// <param name="descendInto">Called for each visited descendant: return false to skip that control's children (the control
        /// itself is still matched), for example <c>c =&gt; c is not UserControl</c>. Null visits the whole tree.</param>
        /// <returns>The matching descendants in depth-first pre-order (see <see cref="GetAllObjs{T}(System.Windows.Forms.Control)"/>).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ctrl"/> is null.</exception>
        public static IEnumerable<T> GetAllObjs<T>(this System.Windows.Forms.Control ctrl, Func<System.Windows.Forms.Control, bool> descendInto) where T : System.Windows.Forms.Control
            => ctrl is null ? throw new ArgumentNullException(nameof(ctrl)) : Descendants<T>(ctrl, descendInto);

        // Iterative depth-first pre-order walk below the root (children pushed in reverse so they come out in collection order)
        private static IEnumerable<T> Descendants<T>(System.Windows.Forms.Control root, Func<System.Windows.Forms.Control, bool> descendInto) where T : System.Windows.Forms.Control
        {
            var stack = new Stack<System.Windows.Forms.Control>();
            PushChildren(stack, root);
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                if (c is T match)
                {
                    yield return match;
                }
                if (descendInto?.Invoke(c) ?? true)
                {
                    PushChildren(stack, c);
                }
            }
        }

        // Push the current children of a control, last first
        private static void PushChildren(Stack<System.Windows.Forms.Control> stack, System.Windows.Forms.Control parent)
        {
            var ctrls = parent.Controls;
            for (var i = ctrls.Count - 1; i >= 0; i--)
            {
                stack.Push(ctrls[i]);
            }
        }

        /// <summary>
        /// Tạo list item cho dropdownlist từ các file trong folder.
        /// </summary>
        /// <param name="ddl">Dropdownlist.</param>
        /// <param name="path">Folder path.</param>
        public static void GetItemListFromFilesInFolderAdv(this YANDdl ddl, string path)
        {
            ddl.Items.Clear();
            if (System.IO.Directory.Exists(path)) // qualified: .NET 7+ also has Path.Exists
            {
                foreach (var file in GetFiles(path))
                {
                    ddl.Items.Add(GetFileNameWithoutExtension(file));
                }
            }
        }
    }
}
