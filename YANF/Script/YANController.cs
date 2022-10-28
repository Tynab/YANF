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
        /// <param name="type">Loại control cần get.</param>
        /// <returns>Control list.</returns>
        public static IEnumerable<System.Windows.Forms.Control> GetAllObjs(this System.Windows.Forms.Control ctrl, Type type)
        {
            var ctrls = ctrl.Controls.Cast<System.Windows.Forms.Control>();
            return ctrls.SelectMany(obj => obj.GetAllObjs(type)).Concat(ctrls).Where(c => c.GetType() == type);
        }

        /// <summary>
        /// Tạo list item cho dropdownlist từ các file trong folder.
        /// </summary>
        /// <param name="path">Folder path.</param>
        public static void GetItemListFromFilesInFolderAdv(this YANDdl ddl, string path)
        {
            ddl.Items.Clear();
            if (Exists(path))
            {
                foreach (var file in GetFiles(path))
                {
                    ddl.Items.Add(GetFileNameWithoutExtension(file));
                }
            }
        }
    }
}
