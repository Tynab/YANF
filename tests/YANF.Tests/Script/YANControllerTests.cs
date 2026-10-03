using System;
using System.Linq;
using System.Windows.Forms;
using Xunit;
using YANF.Control;
using YANF.Script;

namespace YANF.Tests.Script
{
    using Control = System.Windows.Forms.Control;

    // GetAllObjs(Type) matches the exact type only (so Demo2 needed a loop per derived type); GetAllObjs<T> matches with "is T"
    public class YANControllerTests
    {
        [Fact]
        public void GetAllObjsT_FindsDerivedTypesAndNestedChildren_InPreOrder() => Sta.Run(() =>
        {
            using var frm = new Form();
            var p1 = new Panel { Name = "p1" };
            var grad = new YANGradPnl { Name = "grad" };
            var l1 = new Label { Name = "l1" };
            var l0 = new Label { Name = "l0" };
            var p2 = new Panel { Name = "p2" };
            var p3 = new FlowLayoutPanel { Name = "p3" };
            var l3 = new Label { Name = "l3" };
            grad.Controls.Add(l1);
            p1.Controls.Add(grad);
            p3.Controls.Add(l3);
            p2.Controls.Add(p3);
            frm.Controls.AddRange(new Control[] { p1, l0, p2 });

            Assert.Equal(new[] { "p1", "grad", "p2", "p3" }, frm.GetAllObjs<Panel>().Select(c => c.Name));
            Assert.Equal(new[] { "l1", "l0", "l3" }, frm.GetAllObjs<Label>().Select(c => c.Name));
            Assert.Equal(new[] { "p1", "grad", "l1", "l0", "p2", "p3", "l3" }, frm.GetAllObjs<Control>().Select(c => c.Name));
            // the root itself is never included
            Assert.Equal(new[] { "grad", "l1" }, p1.GetAllObjs<Control>().Select(c => c.Name));
            Assert.Empty(l1.GetAllObjs<Control>());
            // the typed result needs no cast
            Label first = frm.GetAllObjs<Label>().First();
            Assert.Same(l1, first);
            // the old overload keeps its exact-type semantics
            Assert.Equal(new[] { "p1", "p2" }, frm.GetAllObjs(typeof(Panel)).Select(c => c.Name).OrderBy(n => n));
        });

        [Fact]
        public void GetAllObjsT_DescendInto_SkipsTheChildrenOfRejectedControls() => Sta.Run(() =>
        {
            using var frm = new Form();
            var uc = new UserControl { Name = "uc" };
            uc.Controls.Add(new Label { Name = "inner" });
            var pnl = new Panel { Name = "pnl" };
            pnl.Controls.Add(new Label { Name = "outer" });
            var ddl = new YANDdl { Name = "ddl" };
            frm.Controls.AddRange(new Control[] { uc, pnl, ddl });

            Assert.Contains("inner", frm.GetAllObjs<Label>().Select(c => c.Name));
            Assert.Equal(new[] { "outer" }, frm.GetAllObjs<Label>(c => c is not UserControl).Select(c => c.Name));
            // a rejected control is still matched itself; only its children are skipped
            Assert.Equal(new[] { "uc", "ddl" }, frm.GetAllObjs<UserControl>(c => c is not UserControl).Select(c => c.Name));
            Assert.Equal(frm.GetAllObjs<Control>().Count(), frm.GetAllObjs<Control>(null).Count());
        });

        [Fact]
        public void GetAllObjsT_DeepTree_FindsEveryLevel() => Sta.Run(() =>
        {
            using var root = new Panel();
            var parent = (Control)root;
            for (var i = 0; i < 1000; i++)
            {
                var child = new Panel();
                parent.Controls.Add(child);
                parent = child;
            }
            Assert.Equal(1000, root.GetAllObjs<Panel>().Count());
        });

        [Fact]
        public void GetAllObjsT_NullRoot_ThrowsAtOnce()
        {
            // thrown by the call, not later by the enumeration
            Assert.Throws<ArgumentNullException>(() => ((Control)null).GetAllObjs<Panel>());
            Assert.Throws<ArgumentNullException>(() => ((Control)null).GetAllObjs<Panel>(c => true));
        }
    }
}
