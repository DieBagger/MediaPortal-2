#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

using MediaPortal.Core;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.PluginManager;
using MediaPortal.Configuration;
using MediaPortal.Configuration.Settings;

using FormControl = System.Windows.Forms.Control;

namespace MediaPortal.Manager
{
  public partial class SettingsControl : UserControl
  {
    private struct SectionDetails
    {
      public SettingBase Section;
      public List<SettingBase> Settings;
      public List<FormControl> Controls;
    }

    private bool _languageChange = false;
    private bool _rightToLeft = false;

    public SettingsControl()
    {
      InitializeComponent();

      // localise buttons
      StringId save = new StringId("configuration", "settings.button.save");
      this.buttonSave.Tag = save;
      this.buttonSave.Text = save.ToString();
      this.buttonSave.Enabled = false;

      StringId apply = new StringId("configuration", "settings.button.apply");
      this.buttonApply.Tag = apply;
      this.buttonApply.Text = apply.ToString();
      this.buttonApply.Enabled = false;

      sections.ImageList = new ImageList();
      sections.ImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
      sections.ImageList.TransparentColor = System.Drawing.Color.Transparent;
      sections.ImageList.ImageSize = new System.Drawing.Size(22, 22);

      LoadConfigData();

      ServiceScope.Get<ILocalisation>().LanguageChange += new LanguageChangeHandler(LangageChange);
      CheckRightToLeft();
    }

    public bool Closing()
    {
      if (this.buttonSave.Enabled)
      {
        StringId message = new StringId("configuration", "save_on_exit");
        StringId title = new StringId("configuration", "unsaved_warning");
        DialogResult result = MessageBox.Show(message.ToString(), title.ToString(), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

        switch (result)
        {
          case DialogResult.Yes:
            SaveAll();
            break;
          case DialogResult.Cancel:
            return false;
          default:
            break;
        }
      }
      return true;
    }

    #region private methods
    private void CheckRightToLeft()
    {
      if (ServiceScope.Get<ILocalisation>().CurrentCulture.TextInfo.IsRightToLeft)
      {
        _rightToLeft = true;

        this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;

        this.sections.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
        this.sections.RightToLeftLayout = true;

        this.sectionIcon.Location = new System.Drawing.Point(5, 5);
        this.sectionTitle.Size = new System.Drawing.Size(sectionHeader.Size.Width - sectionIcon.Size.Width - 5, 16);
        this.sectionTitle.Location = new System.Drawing.Point(sectionIcon.Size.Width + 5, 22);
      }
      else
      {
        _rightToLeft = false;

        this.RightToLeft = System.Windows.Forms.RightToLeft.No;

        this.sections.RightToLeft = System.Windows.Forms.RightToLeft.No;
        this.sections.RightToLeftLayout = false;

        this.sectionIcon.Location = new System.Drawing.Point(sectionHeader.Size.Width - sectionIcon.Size.Width - 5, 5);
        this.sectionTitle.Size = new System.Drawing.Size(sectionHeader.Size.Width - sectionIcon.Size.Width - 5, 16);
        this.sectionTitle.Location = new System.Drawing.Point(0, 22);
      }
    }

    private void LoadConfigData()
    {
      foreach (SettingBase setting in ServiceScope.Get<IPluginManager>().GetAllPluginItems<SettingBase>("/Configuration/Settings"))
      {
        if (setting.Type == SettingType.Section)
        {
          TreeNode node = new TreeNode();
          SectionDetails sectionTag = new SectionDetails();
          sectionTag.Section = setting;
          node.Tag = sectionTag;
          node.Text = setting.Text.ToString();
          //node.BackColor = System.Drawing.Color.Transparent;
          if (setting.IconSmall != null)
          {
            node.ImageIndex = sections.ImageList.Images.Count;
            node.SelectedImageIndex = sections.ImageList.Images.Count;
            AddPNGToImageList(sections.ImageList, setting.IconSmall);
          }
          foreach (SettingBase subsetting in ServiceScope.Get<IPluginManager>().GetAllPluginItems<SettingBase>("/Configuration/Settings/" + setting.Id))
          {
            if (subsetting.Type == SettingType.Section)
            {
              TreeNode subnode = new TreeNode();
              SectionDetails subSectionTag = new SectionDetails();
              subSectionTag.Section = subsetting;
              subnode.Tag = subSectionTag;
              subnode.Text = subsetting.Text.ToString();
              //subnode.BackColor = System.Drawing.Color.Transparent;
              node.Nodes.Add(subnode);
              if (setting.IconSmall != null)
              {
                subnode.ImageIndex = sections.ImageList.Images.Count;
                subnode.SelectedImageIndex = sections.ImageList.Images.Count;
                AddPNGToImageList(sections.ImageList, subsetting.IconSmall);
              }
            }
          }
          sections.Nodes.Add(node);
        }
      }
    }

    private void DrawSettings()
    {
      sectionTitle.Text = sections.SelectedNode.Text;
      sectionIcon.Image = Image.FromFile(((SectionDetails)sections.SelectedNode.Tag).Section.IconLarge);
      sectionSettings.Controls.Clear();
      if (sections.SelectedNode.Tag != null && sections.SelectedNode.Tag is SectionDetails)
      {
        if (((SectionDetails)sections.SelectedNode.Tag).Controls == null)
        {
          BuildSettings();
        }

        foreach (FormControl settingControl in ((SectionDetails)sections.SelectedNode.Tag).Controls)
        {
          sectionSettings.Controls.Add(settingControl);
        }
      }
    }

    private void BuildSettings()
    {
      if (sections.SelectedNode.Tag != null && sections.SelectedNode.Tag is SectionDetails)
      {
        SectionDetails sectionTag = (SectionDetails)sections.SelectedNode.Tag;

        if (sectionTag.Settings == null)
        {
          string location = "/Configuration/Settings/";
          if (sections.SelectedNode.Parent != null
            && sections.SelectedNode.Parent.Tag != null
            && sections.SelectedNode.Parent.Tag is SectionDetails)
          {
            location += ((SectionDetails)sections.SelectedNode.Parent.Tag).Section.Id + "/";
          }

          location += sectionTag.Section.Id;

          sectionTag.Settings = ServiceScope.Get<IPluginManager>().GetAllPluginItems<SettingBase>(location);
        }

        sectionTag.Controls = new List<FormControl>();

        int linePos = 20;
        int lineHeight = 25;
        int margin = 12;

        int startColumnOne = 0;
        int startColumnTwo = (int)(sectionSettings.Size.Width * 0.64);
        int widthColumnOne = startColumnTwo;
        int widthColumnTwo = sectionSettings.Size.Width - startColumnTwo;

        if (_rightToLeft)
        {
          startColumnOne = (int)(sectionSettings.Size.Width * 0.36);
          startColumnTwo = 0;
          widthColumnOne = sectionSettings.Size.Width - startColumnOne;
          widthColumnTwo = startColumnOne;
        }

        foreach (SettingBase setting in sectionTag.Settings)
        {
          switch (setting.Type)
          {
            case SettingType.Heading:
              Label heading = new Label();
              heading.AutoSize = false;
              heading.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
              heading.Location = new System.Drawing.Point(startColumnOne, linePos);
              heading.Name = "heading" + linePos.ToString();
              heading.Size = new System.Drawing.Size(widthColumnOne, 13);
              heading.TabIndex = 1;
              heading.Text = setting.Text.ToString();
              sectionTag.Controls.Add(heading);
              linePos += lineHeight;
              break;

            case SettingType.YesNo:
              if (setting is YesNo)
              {
                Label yesnoLabel = new Label();
                yesnoLabel.AutoSize = false;
                yesnoLabel.Location = new System.Drawing.Point(startColumnOne, linePos - 2);
                yesnoLabel.Margin = new System.Windows.Forms.Padding(margin, 3, margin, 3);
                yesnoLabel.Name = "heading" + linePos.ToString();
                yesnoLabel.Size = new System.Drawing.Size(widthColumnOne, 13);
                yesnoLabel.TabIndex = 1;
                yesnoLabel.Text = setting.Text.ToString();
                sectionTag.Controls.Add(yesnoLabel);

                CheckBox yesno = new CheckBox();
                yesno.AutoSize = true;
                yesno.Location = new System.Drawing.Point(startColumnTwo, linePos - 2);
                yesno.Name = "checkBox" + linePos.ToString();
                yesno.Size = new System.Drawing.Size(15, 14);
                yesno.TabIndex = 1;
                yesno.UseVisualStyleBackColor = true;
                yesno.Checked = ((YesNo)setting).Yes;
                yesno.Tag = setting;
                yesno.CheckedChanged += new System.EventHandler(this.YesNoChange);
                help.SetToolTip(yesno, setting.Help.ToString());
                sectionTag.Controls.Add(yesno);
              }
              break;

            case SettingType.SingleSelectionList:
              if (setting is SingleSelectionList && ((SingleSelectionList)setting).Items != null)
              {
                Label selectionLabel = new Label();
                selectionLabel.AutoSize = false;
                selectionLabel.Location = new System.Drawing.Point(startColumnOne, linePos - 2);
                selectionLabel.Margin = new System.Windows.Forms.Padding(margin, 3, margin, 3);
                selectionLabel.Padding = new System.Windows.Forms.Padding(margin, 0, margin, 0);
                selectionLabel.Name = "heading" + linePos.ToString();
                selectionLabel.Size = new System.Drawing.Size(widthColumnOne, 13);
                selectionLabel.TabIndex = 1;
                selectionLabel.Text = setting.Text.ToString();
                sectionTag.Controls.Add(selectionLabel);

                // check number of items
                if (((SingleSelectionList)setting).Items.Count > 3)
                {
                  // if more than 3 items use drop down box
                  ComboBox selection = new ComboBox();
                  selection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                      | System.Windows.Forms.AnchorStyles.Right)));
                  selection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
                  selection.FormattingEnabled = false;
                  selection.Location = new System.Drawing.Point(startColumnTwo, linePos);
                  selection.Name = "comboBox" + linePos.ToString();
                  selection.Size = new System.Drawing.Size(widthColumnTwo, 21);
                  selection.TabIndex = 3;
                  help.SetToolTip(selection, setting.Help.ToString());

                  foreach (StringId item in ((SingleSelectionList)setting).Items)
                  {
                    selection.Items.Add(item.ToString());
                  }
                  selection.SelectedIndex = ((SingleSelectionList)setting).Selected;
                  selection.Tag = setting;
                  selection.SelectedIndexChanged += new System.EventHandler(this.SingleSelectionListChange);

                  sectionTag.Controls.Add(selection);
                  linePos += lineHeight;
                }
                else
                {
                  // 3 or less items use radio buttons
                  FlowLayoutPanel radioPanel = new FlowLayoutPanel();
                  radioPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                      | System.Windows.Forms.AnchorStyles.Right)));
                  radioPanel.Location = new System.Drawing.Point(startColumnTwo, linePos - 2);
                  radioPanel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 0);
                  int lines = 0;
                  int index = 0;
                  int selected = ((SingleSelectionList)setting).Selected;
                  foreach (StringId item in ((SingleSelectionList)setting).Items)
                  {
                    RadioButton radioButton = new RadioButton();
                    radioButton.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
                    radioButton.Text = item.ToString();
                    radioButton.Tag = index;
                    if (index == selected)
                      radioButton.Checked = true;
                    radioButton.CheckedChanged += new System.EventHandler(this.SingleSelectionListChange);
                    help.SetToolTip(radioButton, setting.Help.ToString());
                    radioPanel.Controls.Add(radioButton);
                    lines++;
                    index++;
                  }
                  radioPanel.Size = new System.Drawing.Size(widthColumnTwo, lines * lineHeight);
                  radioPanel.Tag = setting;
                  sectionTag.Controls.Add(radioPanel);
                  linePos += lines * lineHeight;
                }
              }
              break;
          }
        }

        sections.SelectedNode.Tag = sectionTag;
      }
    }

    private void SaveAll()
    {
      foreach (TreeNode node in sections.Nodes)
      {
        foreach (TreeNode subnode in node.Nodes)
        {
          if (subnode.Tag != null
            && subnode.Tag is SectionDetails
            && ((SectionDetails)subnode.Tag).Settings != null)
          {
            foreach (SettingBase setting in ((SectionDetails)subnode.Tag).Settings)
              setting.Save();
          }
        }
        if (node.Tag != null
          && node.Tag is SectionDetails
          && ((SectionDetails)node.Tag).Settings != null)
        {
          foreach (SettingBase setting in ((SectionDetails)node.Tag).Settings)
            setting.Save();
        }
      }
    }

    private void ApplyAll()
    {
      foreach (TreeNode node in sections.Nodes)
      {
        // Apply on all settings in this node
        if (node.Tag != null
          && node.Tag is SectionDetails
          && ((SectionDetails)node.Tag).Settings != null)
        {
          foreach (SettingBase setting in ((SectionDetails)node.Tag).Settings)
            setting.Apply();
        }

        // Apply on all settings in subnodes
        foreach (TreeNode subnode in node.Nodes)
        {
          if (subnode.Tag != null
            && subnode.Tag is SectionDetails
            && ((SectionDetails)subnode.Tag).Settings != null)
          {
            foreach (SettingBase setting in ((SectionDetails)subnode.Tag).Settings)
              setting.Apply();
          }
        }
      }

      // Redraw language strings on all controls
      if (_languageChange)
      {
        // update text on buttons
        if (this.buttonSave.Tag is StringId)
          this.buttonSave.Text = ((StringId)this.buttonSave.Tag).ToString();

        if (this.buttonApply.Tag is StringId)
          this.buttonApply.Text = ((StringId)this.buttonApply.Tag).ToString();


        // update text in section tree
        // all section details controls need to be cleared and redrawn
        foreach (TreeNode node in sections.Nodes)
        {
          if (node.Tag is SectionDetails)
          {
            SectionDetails details = (SectionDetails)node.Tag;
            node.Text = details.Section.Text.ToString();
            details.Controls = null;
            node.Tag = details;
          }

          foreach (TreeNode subnode in node.Nodes)
          {
            if (subnode.Tag is SectionDetails)
            {
              SectionDetails details = (SectionDetails)subnode.Tag;
              subnode.Text = details.Section.Text.ToString();
              details.Controls = null;
              subnode.Tag = details;
            }
          }
        }

        // Clear all help text
        help.RemoveAll();

        // redraw currently selected section details
        DrawSettings();

        _languageChange = false;
      }
    }
    #endregion

    #region events
    private void LangageChange(object o)
    {
      _languageChange = true;
      CheckRightToLeft();
    }

    #region static controls
    private void sections_AfterSelect(object sender, TreeViewEventArgs e)
    {
      DrawSettings();
    }

    private void buttonSave_Click(object sender, EventArgs e)
    {
      SaveAll();

      this.buttonSave.Enabled = false;
    }

    private void buttonApply_Click(object sender, EventArgs e)
    {
      ApplyAll();

      this.buttonApply.Enabled = false;
    }
    #endregion

    #region settings
    private void YesNoChange(object sender, EventArgs e)
    {
      if (sender != null
        && sender is CheckBox
        && ((CheckBox)sender).Tag != null
        && ((CheckBox)sender).Tag is YesNo)
      {
        ((YesNo)((CheckBox)sender).Tag).Yes = ((CheckBox)sender).Checked;
        this.buttonSave.Enabled = true;
        this.buttonApply.Enabled = true;
      }
    }

    private void SingleSelectionListChange(object sender, EventArgs e)
    {
      if (sender != null)
      {
        if (sender is ComboBox
          && ((ComboBox)sender).Tag is SingleSelectionList)
        {
          ((SingleSelectionList)((ComboBox)sender).Tag).Selected = ((ComboBox)sender).SelectedIndex;
          this.buttonSave.Enabled = true;
          this.buttonApply.Enabled = true;
        }

        if (sender is RadioButton
          && ((RadioButton)sender).Enabled == true
          && ((RadioButton)sender).Tag is int
          && ((RadioButton)sender).Parent != null
          && ((RadioButton)sender).Parent is FlowLayoutPanel)
        {
          ((SingleSelectionList)((FlowLayoutPanel)((RadioButton)sender).Parent).Tag).Selected = (int)((RadioButton)sender).Tag;
          this.buttonSave.Enabled = true;
          this.buttonApply.Enabled = true;
        }
      }
    }
    #endregion
    #endregion

    #region ImageListBugFix

    [DllImport("comctl32.dll")]
    static extern bool ImageList_Add(IntPtr hImageList, IntPtr hBitmap, IntPtr hMask);
    [DllImport("kernel32.dll")]
    static extern bool RtlMoveMemory(IntPtr dest, IntPtr source, int dwcount);
    [DllImport("gdi32.dll")]
    static extern IntPtr CreateDIBSection(IntPtr hdc, [In, MarshalAs(UnmanagedType.LPStruct)] BitmapInfo pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

    [StructLayout(LayoutKind.Explicit)]
    private class BitmapInfo
    {
      [FieldOffset(0)]
      public Int32 biSize;
      [FieldOffset(4)]
      public Int32 biWidth;
      [FieldOffset(8)]
      public Int32 biHeight;
      [FieldOffset(12)]
      public Int16 biPlanes;
      [FieldOffset(14)]
      public Int16 biBitCount;
      [FieldOffset(16)]
      public Int32 biCompression;
      [FieldOffset(20)]
      public Int32 biSizeImage;
      [FieldOffset(24)]
      public Int32 biXPelsPerMeter;
      [FieldOffset(28)]
      public Int32 biYPelsPerMeter;
      [FieldOffset(32)]
      public Int32 biClrUsed;
      [FieldOffset(36)]
      public Int32 biClrImportant;
      [FieldOffset(40)]
      public Int32 colors;
    };

    private void AddPNGToImageList(ImageList imageList, string fileName)
    {
      Bitmap bitmap = new Bitmap(fileName);
      IntPtr hBitmap, ppvBits;
      BitmapInfo bitmapInfo = new BitmapInfo();

      bitmapInfo.biSize = 40;
      bitmapInfo.biBitCount = 32;
      bitmapInfo.biPlanes = 1;
      bitmapInfo.biWidth = bitmap.Width;
      bitmapInfo.biHeight = bitmap.Height;
      bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
      hBitmap = CreateDIBSection(new IntPtr(0), bitmapInfo, 0, out ppvBits, new IntPtr(0), 0);
      Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
      BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
      IntPtr pixels = bitmapData.Scan0;
      RtlMoveMemory(ppvBits, pixels, bitmap.Height * bitmapData.Stride);
      bitmap.UnlockBits(bitmapData);
      ImageList_Add(imageList.Handle, hBitmap, new IntPtr(0));
    }
    #endregion
  }
}
