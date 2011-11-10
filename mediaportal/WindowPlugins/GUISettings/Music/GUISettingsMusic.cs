#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System.Threading;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.Services;
using MediaPortal.Threading;

namespace MediaPortal.GUI.Settings
{
  /// <summary>
  /// Summary description for Class1.
  /// </summary>
  public class GUISettingsMusic : GUIInternalWindow
  {
    [SkinControl(2)] protected GUIButtonControl btnNowPlaying= null;
    [SkinControl(8)] protected GUIButtonControl btnPlaylist= null;
    [SkinControl(10)] protected GUIButtonControl btnDeletealbuminfo= null;
    [SkinControl(13)] protected GUIButtonControl btnDeletealbum = null;
    [SkinControl(35)] protected GUIButtonControl btnExtensions = null;
    [SkinControl(40)] protected GUIButtonControl btnFolders = null;
    [SkinControl(41)] protected GUIButtonControl btnDatabase = null;
    
    
    private string _section = "music";

    public GUISettingsMusic()
    {
      GetID = (int)Window.WINDOW_SETTINGS_MUSIC;
    }

    private void LoadSettings()
    {
      //using (Profile.Settings xmlreader = new Profile.MPSettings())
      //{
      //  btnAutoshuffle.Selected = xmlreader.GetValueAsBool("musicfiles", "autoshuffle", true);
      //}
    }

    private void SaveSettings()
    {
      //using (Profile.Settings xmlreader = new Profile.MPSettings())
      //{
      //  xmlreader.SetValueAsBool("musicfiles", "autoshuffle",btnAutoshuffle.Selected);
      //}
    }

    public override bool Init()
    {
      return Load(GUIGraphicsContext.Skin + @"\SettingsMyMusic.xml");
    }

    protected override void OnPageLoad()
    {
      base.OnPageLoad();
      LoadSettings();
    }

    protected override void  OnPageDestroy(int new_windowId)
    {
      SaveSettings();
 	    base.OnPageDestroy(new_windowId);
    }

    public override void OnAction(Action action)
    {
      switch (action.wID)
      {
        case Action.ActionType.ACTION_PREVIOUS_MENU:
          {
            GUIWindowManager.ShowPreviousWindow();
            return;
          }
      }
      base.OnAction(action);
    }

    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      if (control == btnNowPlaying)
      {
        OnNowPlaying();
      }

      if (control == btnPlaylist)
      {
        OnPlayList();
      }
      
      if (control == btnDeletealbum)
      {
        OnDeleteAlbum();
      }

      if (control == btnDeletealbuminfo)
      {
        OnDeleteAlbumInfo();
      }

      if (control == btnFolders)
      {
        OnFolders();
      }

      if (control == btnExtensions)
      {
        OnExtensions();
      }

      if (control == btnDatabase)
      {
        OnDatabase();
      }

      base.OnClicked(controlId, control, actionType);
    }

    private void OnNowPlaying()
    {
      GUISettingsMusicNowPlaying dlg = (GUISettingsMusicNowPlaying)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_MUSICNOWPLAYING);
      if (dlg == null)
      {
        return;
      }
      GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_MUSICNOWPLAYING);
    }

    private void OnDeleteAlbum ()
    {
      MusicDatabaseReorg dbreorg = new MusicDatabaseReorg();
      dbreorg.DeleteSingleAlbum();
    }

    private void OnDeleteAlbumInfo()
    {
      MusicDatabaseReorg dbreorg = new MusicDatabaseReorg();
      dbreorg.DeleteAlbumInfo();
    }

    private void OnReorgDb()
    {
      GUIDialogYesNo dlgYesNo = (GUIDialogYesNo)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_YES_NO);
      if (null != dlgYesNo)
      {
        dlgYesNo.SetHeading(333);
        dlgYesNo.SetLine(1, "");
        dlgYesNo.SetLine(2, "");
        dlgYesNo.SetLine(3, "");
        dlgYesNo.DoModal(GetID);

        if (dlgYesNo.IsConfirmed)
        {
          MusicDatabaseReorg reorg = new MusicDatabaseReorg(GetID);
          Work work = new Work(new DoWorkHandler(reorg.ReorgAsync));
          work.ThreadPriority = ThreadPriority.Lowest;
          GlobalServiceProvider.Get<IThreadPool>().Add(work, QueuePriority.Low);
        }
      }
    }

    private void OnFolders()
    {
      GUIShareFolders dlg = (GUIShareFolders)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_FOLDERS);
      if (dlg == null)
      {
        return;
      }
      dlg.Section = _section;
      GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_FOLDERS);
    }

    private void OnExtensions()
    {
      GUISettingsExtensions dlg = (GUISettingsExtensions)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_EXTENSIONS);
      if (dlg == null)
      {
        return;
      }
      dlg.Section = _section;
      GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_EXTENSIONS);
    }

    private void OnDatabase()
    {
      GUISettingsMusicDatabase dlg = (GUISettingsMusicDatabase)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_MUSICDATABASE);
      if (dlg == null)
      {
        return;
      }
      GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_MUSICDATABASE);
    }

    private void OnPlayList()
    {
      GUISettingsPlaylist dlg = (GUISettingsPlaylist)GUIWindowManager.GetWindow((int)Window.WINDOW_SETTINGS_PLAYLIST);
      if (dlg == null)
      {
        return;
      }
      dlg.Section = _section;
      GUIWindowManager.ActivateWindow((int)Window.WINDOW_SETTINGS_PLAYLIST);
    }
  }
}