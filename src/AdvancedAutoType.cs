using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using KeePass;
using KeePass.Forms;
using KeePass.Plugins;
using KeePass.UI;
using KeePass.Util;
using KeePass.Util.Spr;
using KeePassLib;
using KeePassLib.Collections;
using KeePassLib.Delegates;
using KeePassLib.Utility;
using PluginTools;

namespace AdvancedAutoType
{
  public sealed class AdvancedAutoTypeExt : Plugin
  {
    public static string ExclusionPlaceholder = "{EXCLUDE_ENTRY}";

    #region class members
    private IPluginHost m_host = null;

    private List<PwEntry> m_lExcludedPwEntries = new List<PwEntry>();
    #endregion

    public override bool Initialize(IPluginHost host)
    {
      Terminate();
      m_host = host;

      SprEngine.FilterPlaceholderHints.Add(ExclusionPlaceholder);

      AutoType.FilterCompilePre += AutoType_FilterCompilePre;
      AutoType.SequenceQueriesBegin += AutoType_SequenceQueriesBegin;
      AutoType.SequenceQueriesEnd += AutoType_SequenceQueriesEnd;

      GlobalWindowManager.WindowAdded += OnWindowAdded;
      GlobalWindowManager.WindowRemoved += OnWindowRemoved;

      return true;
    }

    public override void Terminate()
    {
      if (m_host == null) return;

      GlobalWindowManager.WindowAdded -= OnWindowAdded;
      GlobalWindowManager.WindowRemoved -= OnWindowRemoved;

      SprEngine.FilterPlaceholderHints.Remove(ExclusionPlaceholder);

      AutoType.FilterCompilePre -= this.AutoType_FilterCompilePre;
      AutoType.SequenceQueriesEnd -= this.AutoType_SequenceQueriesBegin;
      AutoType.SequenceQueriesEnd -= this.AutoType_SequenceQueriesEnd;

      m_host = null;
    }

    private string AdjustSequence(string sequence, bool bResetPWOnly)
    {
      return sequence.Replace(ExclusionPlaceholder, "");
    }

    private void AutoType_FilterCompilePre(object sender, AutoTypeEventArgs e)
    {
      /* This is only required if the Auto-Type Entry Selection window is NOT shown
			 * If the window is shown, the auto-type sequences are already adjusted
			*/
      e.Sequence = AdjustSequence(e.Sequence, true);
    }

    private static readonly char[] g_vNormToHyphen = new char[] {
			// Sync with UI option name
			'\u2010', // Hyphen
			'\u2011', // Non-breaking hyphen
			'\u2012', // Figure dash
			'\u2013', // En dash
			'\u2014', // Em dash
			'\u2015', // Horizontal bar
			'\u2212' // Minus sign
		};
    internal static string NormalizeWindowText(string str)
    {
      if (string.IsNullOrEmpty(str)) return string.Empty;

      str = str.Trim();

      if (Program.Config.Integration.AutoTypeMatchNormDashes &&
        (str.IndexOfAny(g_vNormToHyphen) >= 0))
      {
        for (int i = 0; i < g_vNormToHyphen.Length; ++i)
          str = str.Replace(g_vNormToHyphen[i], '-');
      }

      return str;
    }

    internal static bool IsMatchWindow(string strWindow, string strFilter)
    {
      if (strWindow == null) { Debug.Assert(false); return false; }
      if (strFilter == null) { Debug.Assert(false); return false; }

      Debug.Assert(NormalizeWindowText(strWindow) == strWindow); // Should be done by caller
      string strF = NormalizeWindowText(strFilter);

      int ccF = strF.Length;
      if ((ccF > 4) && (strF[0] == '/') && (strF[1] == '/') &&
        (strF[ccF - 2] == '/') && (strF[ccF - 1] == '/'))
      {
        try
        {
          string strRx = strF.Substring(2, ccF - 4);
          return Regex.IsMatch(strWindow, strRx, RegexOptions.IgnoreCase);
        }
        catch (Exception) { return false; }
      }

      return StrUtil.SimplePatternMatch(strF, strWindow, StrUtil.CaseIgnoreCmp);
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

        foreach (AutoTypeAssociation a in pe.AutoType.Associations)
        {
          if (a.Sequence.Contains(ExclusionPlaceholder))
          {
            string strFilter = SprEngine.Compile(a.WindowName, sprCtx);
            if (IsMatchWindow(e.TargetWindowTitle, strFilter) && pe.GetAutoTypeEnabled())
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

	private void OnWindowAdded(object sender, GwmWindowEventArgs e)
    {
      if (e.Form is EditAutoTypeItemForm)
      {
        EditAutoTypeItemForm form = e.Form as EditAutoTypeItemForm;
        CustomRichTextBoxEx rtbPlaceholders = Tools.GetControl("m_rtbPlaceholders", form) as CustomRichTextBoxEx;
        rtbPlaceholders.LinkClicked += PlaceholdersLinkClicked;
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
        EditAutoTypeItemForm form = e.Form as EditAutoTypeItemForm;
        CustomRichTextBoxEx rtbPlaceholders = Tools.GetControl("m_rtbPlaceholders", form) as CustomRichTextBoxEx;
        rtbPlaceholders.LinkClicked -= PlaceholdersLinkClicked;
      }
      else if (e.Form is GroupForm)
      {
        SprEngine.FilterPlaceholderHints.Add(ExclusionPlaceholder);
      }
    }

    private void PlaceholdersLinkClicked(object sender, LinkClickedEventArgs e)
    {
      CustomRichTextBoxEx rtbPlaceholders = sender as CustomRichTextBoxEx;
      EditAutoTypeItemForm form = rtbPlaceholders.Parent as EditAutoTypeItemForm;

      CustomRichTextBoxEx rbKeySeq = Tools.GetControl("m_rbKeySeq", form) as CustomRichTextBoxEx;
      if (e.LinkText.Equals(ExclusionPlaceholder) || rbKeySeq.Text.Contains(ExclusionPlaceholder))
      {
        rbKeySeq.Text = ExclusionPlaceholder;
        rbKeySeq.Select(ExclusionPlaceholder.Length, 0);
      }
    }

    public override string UpdateUrl
    {
      get { return @"https://raw.githubusercontent.com/rookiestyle/advancedautotype/master/version.info"; }
    }

    public override Image SmallIcon
    {
      get { return GfxUtil.ScaleImage(Resources.smallicon, 16, 16); }
    }
  }
}
