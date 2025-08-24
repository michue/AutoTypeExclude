using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace PluginTools
{
  public static partial class Tools
  {
    public static object GetField(string field, object obj)
    {
      BindingFlags bf = BindingFlags.Instance | BindingFlags.NonPublic;
      return GetField(field, obj, bf);
    }

    public static object GetField(string field, object obj, BindingFlags bf)
    {
      if (obj == null) return null;
      FieldInfo fi = obj.GetType().GetField(field, bf);
      if (fi == null) return null;
      return fi.GetValue(obj);
    }

    public static Control GetControl(string control)
    {
      return GetControl(control, KeePass.Program.MainForm);
    }

    public static Control GetControl(string control, Control form)
    {
      if (form == null) return null;
      if (string.IsNullOrEmpty(control)) return null;
      Control[] cntrls = form.Controls.Find(control, true);
      if (cntrls.Length == 0) return null;
      return cntrls[0];
    }
  }
}
