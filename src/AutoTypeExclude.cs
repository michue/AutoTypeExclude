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

    #region class members
    private IPluginHost m_host = null;

    private MethodInfo m_miIsMatchWindow = null;
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

      m_miIsMatchWindow = typeof(AutoType).GetMethod("IsMatchWindow", BindingFlags.Static | BindingFlags.NonPublic);

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

        m_rtbPlaceholders = FindControl("m_rtbPlaceholders", m_fEditAutoTypeItemForm) as CustomRichTextBoxEx;
        m_rtbPlaceholders.LinkClicked += PlaceholdersLinkClicked;
      }
      else if (e.Form is GroupForm)
      {
        SprEngine.FilterPlaceholderHints.Remove(ExclusionPlaceholder);
      }
    }

    private void OnWindowRemoved(object sender, GwmWindowEventArgs e)
    {
      if (e.Form is EditAutoTypeItemForm)
      {
        m_rtbPlaceholders.LinkClicked -= PlaceholdersLinkClicked;
        m_rtbPlaceholders = null;
        m_fEditAutoTypeItemForm = null;
      }
      else if (e.Form is GroupForm)
      {
        SprEngine.FilterPlaceholderHints.Add(ExclusionPlaceholder);
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
      /* This is only required if the Auto-Type Entry Selection window is NOT shown
       * If the window is shown, the auto-type sequences are already adjusted
      */
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
