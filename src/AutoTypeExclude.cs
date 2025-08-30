using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using KeePass.Forms;
using KeePass.Plugins;
using KeePass.UI;
using KeePass.Util;
using KeePass.Util.Spr;
using KeePassLib;
using KeePassLib.Collections;
using KeePassLib.Delegates;
using KeePassLib.Utility;

namespace AutoTypeExclude
{
  public sealed class AutoTypeExcludeExt : Plugin
  {
    public static string ExclusionPlaceholder = "{EXCLUDE_ENTRY}";

    private const string Rsa4096PublicKeyXml =
      @"<RSAKeyValue><Modulus>1t+XOJUOXCyTUiTNSIonDFtp/VnXicZsUeyAqb9MmV" +
      @"PnLC+UyZjxJKwv7vsM1Ox1nGYQba2OpgfXrEhzIQ8ov/wjOEPYgMnajppr/NsCJ6" +
      @"79QDvgeZ7k/Ya7mxD4+L+Zkdt/jsQPiy4VIQdaTZ6IiCk5O0yORjD9LxShFajYYQ" +
      @"xJFTnex93aiwM38JZLuNhrImM2hfsUxQdeBrg47bueWF4EyVTwoLr5WAR1GYAg2+" +
      @"5/UdoXln80uM38/2xxMwW01kZjJmjlpCFej4I8ObczCjg6d+P1p4KqKqvqP6nFgN" +
      @"tav4FcstOPamkdXE8gT0yXPkIpIHgl0Z2dsjSX7g7olaauFfS9PukFwGhEP/QIZV" +
      @"W0+YN0Dnp5RCXIA4thl/B7U9THW67TkEfRxl6ft+MfwuC+3AWSCWtqaossBzie1K" +
      @"uD7eaI0A2YoEaubRh3Q64aoTQO9RCyCmk6eDMJRbx1LrCeZ8ADEGRCkrkZyRuuYS" +
      @"5S8aAKxby9AGqOy3PdtkPP2dj3NqR/iqAI/4z13oCUJ3c76KfxvX/JSpg7zgScCQ" +
      @"tm/OAPq3IALtCy+4CxCQv4aEy5ML1++PzCWDcqETaXM2P08znYFaYEsor24RxoA7" +
      @"YdLGhksyQ3MDKioFxwIcn4eJtDzD5b9b56roOvuVSHnw0YXp8Du10uBZlGPo61NR" +
      @"c=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

    #region class members
    private IPluginHost m_host = null;

    private MethodInfo m_miIsMatchWindow = null;

    private FieldInfo m_fiAtConfig = null;
    private FieldInfo m_fiAssocIndex = null;
    private FieldInfo m_fiEditSequenceOnly = null;
    private EditAutoTypeItemForm m_fEditAutoTypeItemForm = null;
    private CustomRichTextBoxEx m_rtbPlaceholders = null;

    private List<PwEntry> m_lExcludedPwEntries = new List<PwEntry>();
    #endregion

    #region plugin interface
    public override Image SmallIcon
    {
      get { return GfxUtil.ScaleImage(Resources.SmallIcon, 16, 16); }
    }

    public override string UpdateUrl
    {
      get { return @"https://raw.githubusercontent.com/michue/autotypeexclude/main/version.info"; }
    }

    public override bool Initialize(IPluginHost host)
    {
      Terminate();
      m_host = host;

      UpdateCheckEx.SetFileSigKey(UpdateUrl, Rsa4096PublicKeyXml);

      m_miIsMatchWindow = typeof(AutoType).GetMethod("IsMatchWindow", BindingFlags.Static | BindingFlags.NonPublic);

      m_fiAtConfig = typeof(EditAutoTypeItemForm).GetField("m_atConfig", BindingFlags.Instance | BindingFlags.NonPublic);
      m_fiAssocIndex = typeof(EditAutoTypeItemForm).GetField("m_iAssocIndex", BindingFlags.Instance | BindingFlags.NonPublic);
      m_fiEditSequenceOnly = typeof(EditAutoTypeItemForm).GetField("m_bEditSequenceOnly", BindingFlags.Instance | BindingFlags.NonPublic);

      GlobalWindowManager.WindowAdded += OnWindowAdded;
      GlobalWindowManager.WindowRemoved += OnWindowRemoved;

      AutoType.FilterCompilePre += AutoType_FilterCompilePre;
      AutoType.SequenceQueriesBegin += AutoType_SequenceQueriesBegin;
      AutoType.SequenceQueriesEnd += AutoType_SequenceQueriesEnd;

      SprEngine.FilterPlaceholderHints.Add(ExclusionPlaceholder);

      return true;
    }

    public override void Terminate()
    {
      if (m_host == null) return;

      SprEngine.FilterPlaceholderHints.Remove(ExclusionPlaceholder);

      AutoType.FilterCompilePre -= this.AutoType_FilterCompilePre;
      AutoType.SequenceQueriesEnd -= this.AutoType_SequenceQueriesBegin;
      AutoType.SequenceQueriesEnd -= this.AutoType_SequenceQueriesEnd;

      GlobalWindowManager.WindowAdded -= OnWindowAdded;
      GlobalWindowManager.WindowRemoved -= OnWindowRemoved;

      m_host = null;
    }
    #endregion

    #region placeholder handling
    public static Control FindControl(string control, Control form)
    {
      if (string.IsNullOrEmpty(control) || form == null) return null;

      Control[] cntrls = form.Controls.Find(control, true);

      if (cntrls.Length == 0) return null;
      else return cntrls[0];
    }

    private void OnWindowAdded(object sender, GwmWindowEventArgs e)
    {
      if (e.Form is EditAutoTypeItemForm)
      {
        m_fEditAutoTypeItemForm = e.Form as EditAutoTypeItemForm;

        if ((bool)m_fiEditSequenceOnly.GetValue(m_fEditAutoTypeItemForm))
        {
          SprEngine.FilterPlaceholderHints.Remove(ExclusionPlaceholder);
        }
        else
        {
          m_rtbPlaceholders = FindControl("m_rtbPlaceholders", m_fEditAutoTypeItemForm) as CustomRichTextBoxEx;
          m_rtbPlaceholders.LinkClicked += PlaceholdersLinkClicked;
        }
      }
    }

    private void OnWindowRemoved(object sender, GwmWindowEventArgs e)
    {
      if (e.Form is EditAutoTypeItemForm)
      {
        AutoTypeConfig atConfig = m_fiAtConfig.GetValue(m_fEditAutoTypeItemForm) as AutoTypeConfig;

        if ((bool)m_fiEditSequenceOnly.GetValue(m_fEditAutoTypeItemForm))
        {
          SprEngine.FilterPlaceholderHints.Add(ExclusionPlaceholder);

          atConfig.DefaultSequence = atConfig.DefaultSequence.Replace(ExclusionPlaceholder, "");
        }
        else
        {
          m_rtbPlaceholders.LinkClicked -= PlaceholdersLinkClicked;
          m_rtbPlaceholders = null;

          AutoTypeAssociation atAssoc = null;
          int assocIndex = (int)m_fiAssocIndex.GetValue(m_fEditAutoTypeItemForm);
          if (assocIndex >= 0) atAssoc = atConfig.GetAt(assocIndex);
          else atAssoc = atConfig.GetAt(atConfig.AssociationsCount - 1);
          if (atAssoc.Sequence.Contains(ExclusionPlaceholder))
            atAssoc.Sequence = ExclusionPlaceholder;
        }

        m_fEditAutoTypeItemForm = null;
      }
    }

    private void PlaceholdersLinkClicked(object sender, LinkClickedEventArgs e)
    {
      CustomRichTextBoxEx rbKeySeq = FindControl("m_rbKeySeq", m_fEditAutoTypeItemForm) as CustomRichTextBoxEx;

      if (e.LinkText.Equals(ExclusionPlaceholder) || rbKeySeq.Text.Contains(ExclusionPlaceholder))
      {
        rbKeySeq.Text = ExclusionPlaceholder;
        rbKeySeq.Select(ExclusionPlaceholder.Length, 0);
      }
    }
    #endregion

    #region auto-type event handlers
    private void AutoType_FilterCompilePre(object sender, AutoTypeEventArgs e)
    {
      // Just in case, replace the sequence during auto-typing (should never happen anyways)
      e.Sequence = e.Sequence.Replace(ExclusionPlaceholder, "");
    }

    private void AutoType_SequenceQueriesBegin(object sender, SequenceQueriesEventArgs e)
    {
      DocumentManagerEx m_docMgr = m_host.MainWindow.DocumentManager;
      List<PwDatabase> lSources = m_docMgr.GetOpenDatabases();

      List<AutoTypeCtx> lCtxs = new List<AutoTypeCtx>();
      PwDatabase pdCurrent = null;
      DateTime dtNow = DateTime.UtcNow;

      EntryHandler eh = delegate (PwEntry pe)
      {
        SprContext sprCtx = new SprContext(pe, pdCurrent,
          SprCompileFlags.NonActive);

        foreach (AutoTypeAssociation assoc in pe.AutoType.Associations)
        {
          if (assoc.Sequence.Contains(ExclusionPlaceholder))
          {
            string strFilter = SprEngine.Compile(assoc.WindowName, sprCtx);

            bool bMatch = (bool)m_miIsMatchWindow.Invoke(null, new object[] { e.TargetWindowTitle, strFilter });

            if (bMatch && pe.GetAutoTypeEnabled())
            {
              pe.AutoType.Enabled = false;
              m_lExcludedPwEntries.Add(pe);
            }
          }
        }

        return true;
      };

      foreach (PwDatabase pd in lSources)
      {
        if ((pd == null) || !pd.IsOpen) continue;
        pdCurrent = pd;
        pd.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, eh);
      }
    }

    private void AutoType_SequenceQueriesEnd(object sender, SequenceQueriesEventArgs e)
    {
      foreach (PwEntry pe in m_lExcludedPwEntries)
      {
        pe.AutoType.Enabled = true;
      }

      m_lExcludedPwEntries.Clear();
    }
    #endregion
  }
}
