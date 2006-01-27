using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Resources;
using System.Windows.Forms;
using AxSHDocVw;

namespace Mpe.Designers
{
  /// <summary>
  /// Summary description for MpeHelpBrowser.
  /// </summary>
  public class MpeHelpBrowser : UserControl, MpeDesigner
  {
    #region Variables

    private Container components = null;
    private MediaPortalEditor mpe;
    private AxWebBrowser browser;

    #endregion

    #region Contructors

    public MpeHelpBrowser(MediaPortalEditor mpe)
    {
      this.mpe = mpe;
      InitializeComponent();
    }

    #endregion

    #region Methods

    public void ShowHelp(FileInfo file)
    {
      if (file == null || file.Exists == false)
      {
        throw new DesignerException("Invalid help file");
      }
      try
      {
        browser.Navigate(FileToUrl(file));
      }
      catch (Exception e)
      {
        MpeLog.Error(e);
        throw new DesignerException(e.Message);
      }
    }

    public void Initialize()
    {
      //
    }

    public void Save()
    {
      //
    }

    public void Cancel()
    {
      //
    }

    public void Destroy()
    {
      browser.Dispose();
    }

    public void Pause()
    {
      mpe.PropertyManager.SelectedResource = null;
      mpe.PropertyManager.HideResourceList();
    }

    public void Resume()
    {
      mpe.PropertyManager.SelectedResource = null;
      mpe.PropertyManager.HideResourceList();
    }

    private string FileToUrl(FileInfo file)
    {
      if (file == null)
      {
        throw new Exception("File cannot be null!");
      }
      return "file:///" + file.FullName.Replace("\\", "/");
    }

    private string FileToUrl(DirectoryInfo file)
    {
      if (file == null)
      {
        throw new Exception("File cannot be null!");
      }
      return "file:///" + file.FullName.Replace("\\", "/");
    }

    private string FileToUrl(string path)
    {
      return "file:///" + path.Replace("\\", "/");
    }

    #endregion

    #region Properties

    public string ResourceName
    {
      get { return "Help"; }
    }

    public bool AllowAdditions
    {
      get { return false; }
    }

    public bool AllowDeletions
    {
      get { return false; }
    }

    #endregion	

    #region Component Designer Generated Code

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (components != null)
        {
          components.Dispose();
        }
      }
      base.Dispose(disposing);
    }

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      ResourceManager resources = new ResourceManager(typeof(MpeHelpBrowser));
      browser = new AxWebBrowser();
      ((ISupportInitialize) (browser)).BeginInit();
      SuspendLayout();
      // 
      // browser
      // 
      browser.Dock = DockStyle.Fill;
      browser.Enabled = true;
      browser.Location = new Point(0, 0);
      browser.OcxState = ((AxHost.State) (resources.GetObject("browser.OcxState")));
      browser.Size = new Size(368, 224);
      browser.TabIndex = 0;
      // 
      // MpeHelpBrowser
      // 
      Controls.Add(browser);
      Name = "MpeHelpBrowser";
      Size = new Size(368, 224);
      ((ISupportInitialize) (browser)).EndInit();
      ResumeLayout(false);
    }

    #endregion
  }
}