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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using MediaPortal.Configuration;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;
using MediaPortal.GUI.View;
using MediaPortal.Player;
using MediaPortal.Playlists;
using MediaPortal.Services;
using MediaPortal.Util;
using MediaPortal.Video.Database;
using Action = MediaPortal.GUI.Library.Action;
using WindowPlugins;
using Layout = MediaPortal.GUI.Library.GUIFacadeControl.Layout;

namespace MediaPortal.GUI.Video
{
  /// <summary>
  /// Summary description for GUIVideoBaseWindow.
  /// </summary>
  public abstract class GUIVideoBaseWindow : WindowPluginBase
  {
    #region Base variables

    protected VideoSort.SortMethod currentSortMethod = VideoSort.SortMethod.Name;
    protected VideoSort.SortMethod currentSortMethodRoot = VideoSort.SortMethod.Name;
    //protected VideoViewHandler handler;
    protected string m_strPlayListPath = string.Empty;
    protected string _currentFolder = string.Empty;
    protected string _lastFolder = string.Empty;
    protected bool m_bPlaylistsLayout = false;
    protected PlayListPlayer playlistPlayer;

    #endregion

    #region SkinControls

    [SkinControl(6)] protected GUIButtonControl btnPlayDVD = null;
    [SkinControl(8)] protected GUIButtonControl btnTrailers = null;
    [SkinControl(9)] protected GUIButtonControl btnSavedPlaylists = null;

    #endregion

    #region Constructor / Destructor

    public GUIVideoBaseWindow()
    {
      playlistPlayer = PlayListPlayer.SingletonPlayer;

      if (handler == null)
      {
        handler = new VideoViewHandler();
      }

      GUIWindowManager.OnNewAction += new OnActionHandler(OnNewAction);
    }

    #endregion

    #region Serialisation

    protected override void LoadSettings()
    {
      base.LoadSettings();
      using (Profile.Settings xmlreader = new Profile.MPSettings())
      {
        int defaultSort = (int)VideoSort.SortMethod.Name;
        if ((handler != null) && (handler.View != null) && (handler.View.Filters != null) &&
            (handler.View.Filters.Count > 0))
        {
          FilterDefinition def = (FilterDefinition)handler.View.Filters[0];
          defaultSort = (int)GetSortMethod(def.DefaultSort);
        }


        currentSortMethod = (VideoSort.SortMethod)xmlreader.GetValueAsInt(SerializeName, "sortmethod", defaultSort);
        currentSortMethodRoot =
          (VideoSort.SortMethod)xmlreader.GetValueAsInt(SerializeName, "sortmethodroot", defaultSort);

        string playListFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        playListFolder += @"\My Playlists";

        m_strPlayListPath = xmlreader.GetValueAsString("movies", "playlists", playListFolder);
        m_strPlayListPath = Util.Utils.RemoveTrailingSlash(m_strPlayListPath);
      }
    }

    protected VideoSort.SortMethod GetSortMethod(string s)
    {
      switch (s.Trim().ToLower())
      {
        case "modified":
          return VideoSort.SortMethod.Modified;
        case "created":
          return VideoSort.SortMethod.Created;
        case "date":
          return VideoSort.SortMethod.Date;
        case "label":
          return VideoSort.SortMethod.Label;
        case "name":
          return VideoSort.SortMethod.Name;
        case "rating":
          return VideoSort.SortMethod.Rating;
        case "size":
          return VideoSort.SortMethod.Size;
        case "year":
          return VideoSort.SortMethod.Year;
      }
      if (!string.IsNullOrEmpty(s))
      {
        Log.Error("GUIVideoBaseWindow::GetSortMethod: Unknown String - " + s);
      }
      return VideoSort.SortMethod.Name;
    }

    protected override void SaveSettings()
    {
      base.SaveSettings();
      using (Profile.Settings xmlwriter = new Profile.MPSettings())
      {
        xmlwriter.SetValue(SerializeName, "sortmethod", (int)currentSortMethod);
        xmlwriter.SetValue(SerializeName, "sortmethodroot", (int)currentSortMethodRoot);
      }
    }

    #endregion

    protected override string SerializeName
    {
      get { return "myvideobase"; }
    }

    protected override bool AllowLayout(Layout layout)
    {
      if ((layout == Layout.Playlist) || (layout == GUIFacadeControl.Layout.AlbumView))
      {
        return false;
      }
      return true;
    }

    protected virtual bool AllowSortMethod(VideoSort.SortMethod method)
    {
      return true;
    }

    protected virtual VideoSort.SortMethod CurrentSortMethod
    {
      get { return currentSortMethod; }
      set { currentSortMethod = value; }
    }

    public override void OnAction(Action action)
    {
      if (action.wID == Action.ActionType.ACTION_SHOW_PLAYLIST)
      {
        GUIWindowManager.ActivateWindow((int)Window.WINDOW_VIDEO_PLAYLIST);
        return;
      }
      base.OnAction(action);
    }

    // Make sure we get all of the ACTION_PLAY events (OnAction only receives the ACTION_PLAY event when 
    // the player is not playing)...
    private void OnNewAction(Action action)
    {
      if ((action.wID == Action.ActionType.ACTION_PLAY
           || action.wID == Action.ActionType.ACTION_MUSIC_PLAY)
          && GUIWindowManager.ActiveWindow == GetID)
      {
        GUIListItem item = facadeLayout.SelectedListItem;

        if (item == null || item.Label == "..")
        {
          return;
        }

        OnClick(-1);
      }
    }

    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      base.OnClicked(controlId, control, actionType);

      if (control == btnSavedPlaylists)
      {
        OnShowSavedPlaylists(m_strPlayListPath);
      }

      if (control == btnPlayDVD)
      {
        ISelectDVDHandler selectDVDHandler;
        if (GlobalServiceProvider.IsRegistered<ISelectDVDHandler>())
        {
          selectDVDHandler = GlobalServiceProvider.Get<ISelectDVDHandler>();
        }
        else
        {
          selectDVDHandler = new SelectDVDHandler();
          GlobalServiceProvider.Add<ISelectDVDHandler>(selectDVDHandler);
        }
        string dvdToPlay = selectDVDHandler.ShowSelectDVDDialog(GetID);
        if (dvdToPlay != null)
        {
          OnPlayDVD(dvdToPlay, GetID);
        }
        return;
      }
    }

    protected void OnShowSavedPlaylists(string _directory)
    {
      VirtualDirectory _virtualDirectory = new VirtualDirectory();
      _virtualDirectory.AddExtension(".m3u");
      _virtualDirectory.AddExtension(".pls");
      _virtualDirectory.AddExtension(".b4s");
      _virtualDirectory.AddExtension(".wpl");

      List<GUIListItem> itemlist = _virtualDirectory.GetDirectoryExt(_directory);
      if (_directory == m_strPlayListPath)
      {
        itemlist.RemoveAt(0);
      }

      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null)
      {
        return;
      }
      dlg.Reset();
      dlg.SetHeading(983); // Saved Playlists

      foreach (GUIListItem item in itemlist)
      {
        Util.Utils.SetDefaultIcons(item);
        dlg.Add(item);
      }

      dlg.DoModal(GetID);

      if (dlg.SelectedLabel == -1)
      {
        return;
      }

      GUIListItem selectItem = itemlist[dlg.SelectedLabel];
      if (selectItem.IsFolder)
      {
        OnShowSavedPlaylists(selectItem.Path);
        return;
      }

      GUIWaitCursor.Show();
      LoadPlayList(selectItem.Path);
      GUIWaitCursor.Hide();
    }

    protected override void UpdateButtonStates()
    {
      base.UpdateButtonStates();

      if (GetID == (int)Window.WINDOW_VIDEO_TITLE)
      {
        GUIPropertyManager.SetProperty("#currentmodule",
                                       String.Format("{0}/{1}", GUILocalizeStrings.Get(100006),
                                                     handler.LocalizedCurrentView));
      }
      else
      {
        GUIPropertyManager.SetProperty("#currentmodule", GetModuleName());
      }

      string strLine = string.Empty;
      switch (CurrentSortMethod)
      {
        case VideoSort.SortMethod.Name:
          strLine = GUILocalizeStrings.Get(365);
          break;
        case VideoSort.SortMethod.Date:
          strLine = GUILocalizeStrings.Get(104);
          break;
        case VideoSort.SortMethod.Size:
          strLine = GUILocalizeStrings.Get(105);
          break;
        case VideoSort.SortMethod.Year:
          strLine = GUILocalizeStrings.Get(366);
          break;
        case VideoSort.SortMethod.Rating:
          strLine = GUILocalizeStrings.Get(367);
          break;
        case VideoSort.SortMethod.Label:
          strLine = GUILocalizeStrings.Get(430);
          break;
        case VideoSort.SortMethod.Unwatched:
          strLine = GUILocalizeStrings.Get(527);
          break;
        case VideoSort.SortMethod.Created:
          strLine = GUILocalizeStrings.Get(1220);
          break;
        case VideoSort.SortMethod.Modified:
          strLine = GUILocalizeStrings.Get(1221);
          break;
      }

      if (btnSortBy != null)
      {
        btnSortBy.Label = strLine;
      }

      if (null != facadeLayout)
        facadeLayout.EnableScrollLabel = CurrentSortMethod == VideoSort.SortMethod.Label ||
                                         CurrentSortMethod == VideoSort.SortMethod.Year ||
                                         CurrentSortMethod == VideoSort.SortMethod.Name
          ;
    }

    protected override void OnClick(int item) {}

    protected override void OnQueueItem(int item) {}

    protected override void OnPageLoad()
    {
      GUIVideoOverlay videoOverlay = (GUIVideoOverlay)GUIWindowManager.GetWindow((int)Window.WINDOW_VIDEO_OVERLAY);
      if ((videoOverlay != null) && (videoOverlay.Focused))
      {
        videoOverlay.Focused = false;
      }

      LoadSettings();

      if (btnSortBy != null)
      {
        btnSortBy.SortChanged += new SortEventHandler(SortChanged);
      }

      base.OnPageLoad();
    }

    protected override void OnPageDestroy(int newWindowId)
    {
      SaveSettings();

      // Save view
      using (Profile.Settings xmlwriter = new Profile.MPSettings())
      {
        // Save only MyVideos window views
        if (GUIVideoFiles.IsVideoWindow(VideoState.StartWindow))
        {
          xmlwriter.SetValue("movies", "startWindow", VideoState.StartWindow.ToString());
          xmlwriter.SetValue("movies", "startview", VideoState.View);
        }
      }

      m_bPlaylistsLayout = false;

      base.OnPageDestroy(newWindowId);
    }

    #region Sort Members

    protected virtual void OnSort()
    {
      SetLabels();
      facadeLayout.Sort(new VideoSort(CurrentSortMethod, CurrentSortAsc));
      UpdateButtonStates();
      SelectCurrentItem();
    }

    #endregion

    protected virtual void SetLabels()
    {
      for (int i = 0; i < facadeLayout.Count; ++i)
      {
        GUIListItem item = facadeLayout[i];
        IMDBMovie movie = item.AlbumInfoTag as IMDBMovie;

        if (movie != null && movie.ID > 0 && !item.IsFolder)
        {
          if (CurrentSortMethod == VideoSort.SortMethod.Name)
          {
            item.Label2 = Util.Utils.SecondsToHMString(movie.RunTime * 60);
          }
          else if (CurrentSortMethod == VideoSort.SortMethod.Year)
          {
            item.Label2 = movie.Year.ToString();
          }
          else if (CurrentSortMethod == VideoSort.SortMethod.Rating)
          {
            item.Label2 = movie.Rating.ToString();
          }
          else if (CurrentSortMethod == VideoSort.SortMethod.Label)
          {
            item.Label2 = movie.DVDLabel.ToString();
          }
          else if (CurrentSortMethod == VideoSort.SortMethod.Size)
          {
            if (item.FileInfo != null)
            {
              item.Label2 = Util.Utils.GetSize(item.FileInfo.Length);
            }
            else
            {
              item.Label2 = Util.Utils.SecondsToHMString(movie.RunTime * 60);
            }
          }
        }
        else
        {
          string strSize1 = string.Empty, strDate = string.Empty;
          if (item.FileInfo != null && !item.IsFolder)
          {
            strSize1 = Util.Utils.GetSize(item.FileInfo.Length);
          }
          if (item.FileInfo != null && !item.IsFolder)
          {
            if (CurrentSortMethod == VideoSort.SortMethod.Modified)
              strDate = item.FileInfo.ModificationTime.ToShortDateString() + " " +
                        item.FileInfo.ModificationTime.ToString("t", CultureInfo.CurrentCulture.DateTimeFormat);
            else
              strDate = item.FileInfo.CreationTime.ToShortDateString() + " " +
                        item.FileInfo.CreationTime.ToString("t", CultureInfo.CurrentCulture.DateTimeFormat);
          }
          if (CurrentSortMethod == VideoSort.SortMethod.Name)
          {
            item.Label2 = strSize1;
          }
          else if (CurrentSortMethod == VideoSort.SortMethod.Created || CurrentSortMethod == VideoSort.SortMethod.Date || CurrentSortMethod == VideoSort.SortMethod.Modified)
          {
            item.Label2 = strDate;
          }
          else
          {
            item.Label2 = strSize1;
          }
        }
      }
    }

    protected override void OnShowSort()
    {
      GUIDialogMenu dlg = (GUIDialogMenu)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_MENU);
      if (dlg == null)
      {
        return;
      }
      dlg.Reset();
      dlg.SetHeading(495); // Sort options

      // Watch for enums in VideoSort.cs
      dlg.AddLocalizedString(365); // name
      dlg.AddLocalizedString(104); // date created (date)
      dlg.AddLocalizedString(105); // size
      dlg.AddLocalizedString(366); // year
      dlg.AddLocalizedString(367); // rating
      dlg.AddLocalizedString(430); // label
      dlg.AddLocalizedString(527); // unwatched
      if (GUIWindowManager.ActiveWindow == (int)Window.WINDOW_VIDEOS)
      {
        dlg.AddLocalizedString(1221); // date modified
        dlg.AddLocalizedString(1220); // date created
      }

      // set the focus to currently used sort method
      dlg.SelectedLabel = (int)CurrentSortMethod;

      // show dialog and wait for result
      dlg.DoModal(GetID);
      if (dlg.SelectedId == -1)
      {
        return;
      }

      CurrentSortAsc = true;
      switch (dlg.SelectedId)
      {
        case 365:
          CurrentSortMethod = VideoSort.SortMethod.Name;
          break;
        case 104:
          CurrentSortMethod = VideoSort.SortMethod.Date;
          CurrentSortAsc = false;
          break;
        case 1221:
          CurrentSortMethod = VideoSort.SortMethod.Modified;
          CurrentSortAsc = false;
          break;
        case 1220:
          CurrentSortMethod = VideoSort.SortMethod.Created;
          CurrentSortAsc = false;
          break;
        case 105:
          CurrentSortMethod = VideoSort.SortMethod.Size;
          break;
        case 366:
          CurrentSortMethod = VideoSort.SortMethod.Year;
          break;
        case 367:
          CurrentSortMethod = VideoSort.SortMethod.Rating;
          break;
        case 430:
          CurrentSortMethod = VideoSort.SortMethod.Label;
          break;
        case 527:
          CurrentSortMethod = VideoSort.SortMethod.Unwatched;
          break;
        default:
          CurrentSortMethod = VideoSort.SortMethod.Name;
          break;
      }

      OnSort();
      GUIControl.FocusControl(GetID, btnSortBy.GetID);
    }

    protected override void LoadDirectory(string path) {}

    protected void LoadPlayList(string strPlayList)
    {
      IPlayListIO loader = PlayListFactory.CreateIO(strPlayList);
      if (loader == null)
      {
        return;
      }
      PlayList playlist = new PlayList();

      if (!loader.Load(playlist, strPlayList))
      {
        TellUserSomethingWentWrong();
        return;
      }

      playlistPlayer.CurrentPlaylistName = Path.GetFileNameWithoutExtension(strPlayList);
      if (playlist.Count == 1)
      {
        Log.Info("GUIVideoFiles: play single playlist item - {0}", playlist[0].FileName);
        if (g_Player.Play(playlist[0].FileName))
        {
          if (Util.Utils.IsVideo(playlist[0].FileName))
          {
            g_Player.ShowFullScreenWindow();
          }
        }
        return;
      }

      // clear current playlist
      playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO).Clear();

      // add each item of the playlist to the playlistplayer
      for (int i = 0; i < playlist.Count; ++i)
      {
        PlayListItem playListItem = playlist[i];
        playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO).Add(playListItem);
      }

      // if we got a playlist
      if (playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO).Count > 0)
      {
        // then get 1st song
        playlist = playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO);
        PlayListItem item = playlist[0];

        // and start playing it
        playlistPlayer.CurrentPlaylistType = PlayListType.PLAYLIST_VIDEO;
        playlistPlayer.Reset();
        playlistPlayer.Play(0);

        // and activate the playlist window if its not activated yet
        if (GetID == GUIWindowManager.ActiveWindow)
        {
          GUIWindowManager.ActivateWindow((int)Window.WINDOW_VIDEO_PLAYLIST);
        }
      }
    }

    private void TellUserSomethingWentWrong()
    {
      GUIDialogOK dlgOK = (GUIDialogOK)GUIWindowManager.GetWindow((int)Window.WINDOW_DIALOG_OK);
      if (dlgOK != null)
      {
        dlgOK.SetHeading(6);
        dlgOK.SetLine(1, 477);
        dlgOK.SetLine(2, string.Empty);
        dlgOK.DoModal(GetID);
      }
    }

    private void OnInfoFile(GUIListItem item) {}

    private void OnInfoFolder(GUIListItem item) {}

    protected override void OnInfo(int iItem) {}

    protected virtual void AddItemToPlayList(GUIListItem pItem)
    {
      if (!pItem.IsFolder)
      {
        //TODO
        if (Util.Utils.IsVideo(pItem.Path) && !PlayListFactory.IsPlayList(pItem.Path))
        {
          PlayListItem playlistItem = new PlayListItem();
          playlistItem.Type = PlayListItem.PlayListItemType.Video;
          playlistItem.FileName = pItem.Path;
          playlistItem.Description = pItem.Label;
          playlistItem.Duration = pItem.Duration;
          playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO).Add(playlistItem);
        }
      }
    }

    private void SortChanged(object sender, SortEventArgs e)
    {
      CurrentSortAsc = e.Order != SortOrder.Descending;

      OnSort();
      //UpdateButtonStates();
      GUIControl.FocusControl(GetID, ((GUIControl)sender).GetID);
    }

    protected void OnPlayDVD(String drive, int parentId)
    {
      ISelectDVDHandler selectDVDHandler;
      if (GlobalServiceProvider.IsRegistered<ISelectDVDHandler>())
      {
        selectDVDHandler = GlobalServiceProvider.Get<ISelectDVDHandler>();
      }
      else
      {
        selectDVDHandler = new SelectDVDHandler();
        GlobalServiceProvider.Add<ISelectDVDHandler>(selectDVDHandler);
      }
      selectDVDHandler.OnPlayDVD(drive, GetID);
    }

    protected void OnPlayFiles(System.Collections.ArrayList filesList)
    {
      playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO_TEMP).Clear();
      playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO).Clear();

      foreach (string file in filesList)
      {
        PlayListItem item = new PlayListItem();
        item.FileName = file;
        item.Type = PlayListItem.PlayListItemType.Video;
        item.Description = file;
        playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO).Add(item);
      }

      if (playlistPlayer.GetPlaylist(PlayListType.PLAYLIST_VIDEO).Count > 0)
      {
        GUIWindowManager.ActivateWindow((int)GUIWindow.Window.WINDOW_VIDEO_PLAYLIST);
        playlistPlayer.CurrentPlaylistType = PlayListType.PLAYLIST_VIDEO;
        playlistPlayer.Reset();
        playlistPlayer.Play(0);
      }
    }
  }
}