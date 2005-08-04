using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Crownwood.Magic.Menus;

using Mpe.Controls;
using Mpe.Controls.Properties;
using Mpe.Designers;
using Mpe.Forms;

namespace Mpe.Forms {
	/// <summary>
	///
	/// </summary>
	public class MpeExplorer : System.Windows.Forms.UserControl {

		#region Variables
		private System.Windows.Forms.TreeView skinTree;
		private System.Windows.Forms.Panel skinTreePanel;
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.ImageList treeImageList;
		private Crownwood.Magic.Menus.PopupMenu contextMenu;
		private MenuCommand menuAdd;
		private MenuCommand menuAddExisting;
		private MenuCommand menuAddNew;
		private MenuCommand menuDelete;
		private MenuCommand menuRename;
		private MenuCommand menuModify;
		private MenuCommand menuModifyScreen;
		private MenuCommand menuModifyScreenWindow;
		private MenuCommand menuModifyScreenDialog;
		private MenuCommand menuModifyScreenOSD;
		private TreeNode rootNode;
		private TreeNode imageNode;
		private TreeNode controlNode;
		private TreeNode screenNode;
		private TreeNode fontNode;
		private TreeNode languageNode;
		private TreeNode selectedNode;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private MediaPortalEditor mpe;
		#endregion

		#region Constructors
		public MpeExplorer(MediaPortalEditor mpe) {
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			this.mpe = mpe;
			contextMenu = new PopupMenu();
			menuAdd = new MenuCommand("Add");
			menuAddExisting = new MenuCommand("Add Existing...");
			menuAddNew = new MenuCommand("Add New...");
			menuRename = new MenuCommand("Rename");
			menuDelete = new MenuCommand("Delete");
			menuAdd.MenuCommands.Add(menuAddExisting);
			menuAdd.MenuCommands.Add(menuAddNew);
			
			menuModify = new MenuCommand("Modify");
			menuModifyScreen = new MenuCommand("Screen Type");
			menuModifyScreenWindow = new MenuCommand("Window");
			menuModifyScreenDialog = new MenuCommand("Dialog");
			menuModifyScreenOSD = new MenuCommand("OnScreenDisplay");
			menuModifyScreen.MenuCommands.AddRange(new MenuCommand[] { menuModifyScreenWindow, menuModifyScreenDialog, menuModifyScreenOSD });
			menuModify.MenuCommands.Add(menuModifyScreen);

			contextMenu.MenuCommands.Add(menuAdd);
			contextMenu.MenuCommands.Add(new MenuCommand("-"));
			contextMenu.MenuCommands.Add(menuModify);
			contextMenu.MenuCommands.Add(new MenuCommand("-"));
			contextMenu.MenuCommands.Add(menuRename);
			contextMenu.MenuCommands.Add(menuDelete);
		}
		#endregion

		#region Properties
		protected MpeStatusBar StatusBar {
			get {
				return mpe.StatusBar;
			}
		}
		protected MpeParser Parser {
			get {
				return mpe.Parser;
			}
			set {
				mpe.Parser = value;
			}
		}

		public bool IsSkinLoaded {
			get {
				if (rootNode == null)
					return false;
				return true;
			}
		}
		#endregion

		#region Methods
		public void CloseSkin() {
			if (Parser != null) {
				Parser.Destroy();
				Parser = null;
			}
			skinTree.Nodes.Clear();
			rootNode = null;
			controlNode = null;
			imageNode = null;
			fontNode = null;
			languageNode = null;
			screenNode = null;
			System.GC.Collect();
		}
		public void LoadSkin(DirectoryInfo skinDir) {
			try {
				// Cleanup used resources
				CloseSkin();
				// Initialize a parser to get the skin info
				Parser = new MpeParser(skinDir, new DirectoryInfo(MediaPortalEditor.Global.Preferences.MediaPortalDir));
				Parser.Load();

				// Create rootNode
				rootNode = skinTree.Nodes.Add("Skin (" + Parser.SkinName + ")");
				rootNode.ImageIndex = 0;
				rootNode.SelectedImageIndex = 0;
				// Load skin resources
				LoadControls();
				LoadFonts();
				LoadImages();
				LoadLanguages();
				LoadScreens();
				// Setup tree
				skinTree.Scrollable = true;
				skinTree.Nodes[0].Expand();
				skinTree.Enabled = true;
				skinTree.Focus();	
			} catch (MpeParserException spe) {
				MpeLog.Debug(spe);
				MpeLog.Error(spe);
			} catch (Exception e) {
				MpeLog.Debug(e);
				MpeLog.Error(e);
			} 
		}
		protected void LoadImages() {
			if (imageNode == null) {
				imageNode = rootNode.Nodes.Add("Images");
				imageNode.ImageIndex = 13;
				imageNode.SelectedImageIndex = 13;
			}
			imageNode.Nodes.Clear();
			FileInfo[] images = Parser.ImageFiles;
			for (int i = 0; i < images.Length; i++) {
				TreeNode n = imageNode.Nodes.Add(images[i].Name);
				n.Tag = images[i];
				n.ImageIndex = 14;
				n.SelectedImageIndex = 14;
			}
			imageNode.Text = "Images (" + images.Length + ")";
		}
		protected void LoadFonts() {
			if (fontNode == null) {
				fontNode = rootNode.Nodes.Add("Fonts");
				fontNode.ImageIndex = 11;
				fontNode.SelectedImageIndex = 11;
			}
			fontNode.Nodes.Clear();
			string[] fonts = Parser.FontNames;
			for (int i = 0; i < fonts.Length; i++) {
				TreeNode n = fontNode.Nodes.Add(fonts[i]);
				n.ImageIndex = 12;
				n.SelectedImageIndex = 12;
			}
			fontNode.Text = "Fonts (" + fonts.Length + ")";
		}
		protected void LoadControls() {
			// Load Controls into tree
			if (controlNode == null) {
				controlNode = rootNode.Nodes.Add("Controls");
				controlNode.ImageIndex = 2;
				controlNode.SelectedImageIndex = 2;
			}
			controlNode.Nodes.Clear();
			MpeControlType[] keys = Parser.ControlKeys;
			for (int i = 0; i < keys.Length; i++) {
				TreeNode n = controlNode.Nodes.Add(keys[i].DisplayName);
				n.Tag = keys[i];
				if (keys[i].IsKnown) {
					n.ImageIndex = 3;
					n.SelectedImageIndex = 3;
				} else {
					n.ImageIndex = 4;
					n.SelectedImageIndex = 4;
				}
			}
			controlNode.Text = "Controls (" + keys.Length + ")";
		}
		protected void LoadScreens() {
			// Load Screens into tree
			if (screenNode == null) {
				screenNode = rootNode.Nodes.Add("Screens");
				screenNode.ImageIndex = 5;
				screenNode.SelectedImageIndex = 5;
			}
			screenNode.Nodes.Clear();
			MpeScreenInfo[] screens = Parser.Screens;
			for (int i = 0; i < screens.Length; i++) {
				TreeNode n = screenNode.Nodes.Add(screens[i].File.Name);
				n.Tag = screens[i];
				if (screens[i].Type == MpeScreenType.Dialog) {
					n.ImageIndex = 7;
					n.SelectedImageIndex = 7;
				} else if (screens[i].Type == MpeScreenType.OnScreenDisplay) {
					n.ImageIndex = 8;
					n.SelectedImageIndex = 8;
				} else {
					n.ImageIndex = 6;
					n.SelectedImageIndex = 6;
				}
			}
			screenNode.Text = "Screens (" + screens.Length + ")";
		}
		protected void LoadLanguages() {
			if (languageNode == null) {
				languageNode = rootNode.Nodes.Add("Strings");
				languageNode.ImageIndex = 9;
				languageNode.SelectedImageIndex = 9;
			}
			languageNode.Nodes.Clear();
			Hashtable languages = Parser.StringTables;
			IEnumerator e = languages.Keys.GetEnumerator();
			while (e.MoveNext()) {
				string s = (string)e.Current;
				TreeNode n = languageNode.Nodes.Add(s);
				n.Tag = Parser.GetStringTable(s);
				n.ImageIndex = 10;
				n.SelectedImageIndex = 10;
			}
			languageNode.Text = "Strings (" + languages.Count + ")";
		}
		protected bool UpdateContextMenu() {
			if (selectedNode == null || selectedNode == rootNode) {
				return false;
			}
			skinTree.SelectedNode = selectedNode;
			menuAdd.Enabled = false;
			menuAddExisting.Enabled = false;
			menuAddNew.Enabled = false;
			menuRename.Enabled = false;
			menuDelete.Enabled = false;
			menuModify.Enabled = false;
			menuModifyScreen.Enabled = false;
			menuModifyScreenWindow.Enabled = false;
			menuModifyScreenDialog.Enabled = false;
			menuModifyScreenOSD.Enabled = false;
			if (selectedNode == imageNode) {
				menuAdd.Enabled = true;
				menuAddExisting.Enabled = true;
			} else if (selectedNode == screenNode) {
				menuAdd.Enabled = true;
				menuAddExisting.Enabled = true;
				menuAddNew.Enabled = true;
			} else if (selectedNode == fontNode) {
				menuAdd.Enabled = true;
				menuAddNew.Enabled = true;
			} else if (selectedNode.Parent == screenNode) {
				menuRename.Enabled = true;
				menuDelete.Enabled = true;
				menuModify.Enabled = true;
				menuModifyScreen.Enabled = true;
				MpeScreenInfo info = (MpeScreenInfo)selectedNode.Tag;
				if (info.Type == MpeScreenType.Window) {
					menuModifyScreenWindow.Checked = true;
					menuModifyScreenDialog.Checked = false;
					menuModifyScreenDialog.Enabled = true;
					//menuModifyScreenOSD.Checked = false;
					//menuModifyScreenOSD.Enabled = true;
				} else if (info.Type == MpeScreenType.Dialog) {
					menuModifyScreenDialog.Checked = true;
					//menuModifyScreenOSD.Checked = false;
					//menuModifyScreenOSD.Enabled = true;
					menuModifyScreenWindow.Checked = false;
					menuModifyScreenWindow.Enabled = true;
				} else {
					menuModifyScreenOSD.Checked = true;
					menuModifyScreenDialog.Checked = false;
					menuModifyScreenDialog.Enabled = true;
					menuModifyScreenWindow.Checked = false;
					menuModifyScreenWindow.Enabled = true;
				}
			} else if (selectedNode.Parent == imageNode || selectedNode.Parent == fontNode) {
				menuRename.Enabled = true;
				menuDelete.Enabled = true;
			}
			return true;
		}
		#endregion

		#region Event Handlers
		private void OnDoubleClick(object sender, System.EventArgs e) {
			if (selectedNode == null || selectedNode.Parent == null) {
				return;
			}
			try {
				TreeNode parent = selectedNode.Parent;
				if (parent == screenNode) {
					mpe.AddDesigner(new MpeScreenDesigner(mpe, (MpeScreenInfo)selectedNode.Tag));
				} else if (parent == languageNode) {
					mpe.AddDesigner(new MpeStringDesigner(mpe, Parser.GetStringTable("English"),(MpeStringTable)selectedNode.Tag));
				} else if (parent == imageNode) {
					mpe.AddDesigner(new MpeImageDesigner(mpe, (FileInfo)selectedNode.Tag));
				} else if (parent == controlNode) {
					MpeControl c = Parser.CreateControl((MpeControlType)selectedNode.Tag);
					mpe.AddDesigner(new MpeControlDesigner(mpe, c));
				} else if (parent == fontNode) {
					MpeFont font = new MpeFont(Parser.GetFont(selectedNode.Text));
					mpe.AddDesigner(new FontDesigner(mpe, font));
				}
			} catch (Exception ee) {
				MpeLog.Debug(ee);
				MpeLog.Error(ee);
			}
		}
		private void OnItemDrag(object sender, System.Windows.Forms.ItemDragEventArgs e) {
			if (e.Button == MouseButtons.Left) {
				if (e.Item is TreeNode) {
					TreeNode node = (TreeNode)e.Item;
					if (node.Parent == controlNode && node.Tag is MpeControlType) {
						MpeControlType type = (MpeControlType)node.Tag;
						if (type != MpeControlType.Screen) {
							DoDragDrop(type, DragDropEffects.Copy);
						}
					} else if (node.Parent == imageNode) {
						DoDragDrop((FileInfo)node.Tag, DragDropEffects.Copy);
					} else {
						DoDragDrop(node.Tag, DragDropEffects.Copy);
					}
				}
			}
		}
		private void OnBeforeLabelEdit(object sender, System.Windows.Forms.NodeLabelEditEventArgs e) {
			if (e.Node.Parent != imageNode && e.Node.Parent != screenNode && e.Node.Parent != fontNode) {
				e.CancelEdit = true;
				e.Node.EndEdit(true);
			}
			if (mpe.IsResourceOpen(e.Node.Text)) {
				MpeLog.Warn("The resource file cannot be renamed because it is currently being editted.");
				MessageBox.Show(this,"The resource file cannot be renamed because it is currently being editted.", "Error Renaming File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				e.CancelEdit = true;
				e.Node.EndEdit(true);
				return;
			}
		}
		private void OnAfterLabelEdit(object sender, System.Windows.Forms.NodeLabelEditEventArgs e) {
			if (e.Node == null || e.Label == null) {
				return;
			}
			if (e.Node.Parent == imageNode) {
				int i1 = e.Node.Text.LastIndexOf(".");
				string s1 = (i1 > 0) ? e.Node.Text.Substring(i1) : "";
				int i2 = e.Label.LastIndexOf(".");
				string s2 = (i2 > 0) ? e.Label.Substring(i2) : "";
				if (s1.ToLower().Equals(s2.ToLower()) == false) {
					MessageBox.Show(this, "You cannot change an image file's extension.", "Error Renaming File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					e.CancelEdit = true;
					e.Node.BeginEdit();
					return;
				}
				try {
					FileInfo f = Parser.RenameImageFile(e.Node.Text, e.Label);
					e.Node.EndEdit(false);
					e.Node.Tag = f;
					MpeLog.Info("Renamed image file from [" + e.Node.Text + "] to [" + e.Label + "]");
					skinTree.LabelEdit = false;
					return;
				} catch (Exception ee) {
					MpeLog.Debug(ee);
					if (ee.Message.IndexOf("being used by another process") > 0) {
						MpeLog.Warn("The image file cannot be renamed because it is locked by another process.");
						MessageBox.Show(this, "The image cannot be renamed because it is locked by another process.\n\nIf the image is currently open inside the Media Portal Editor, please close the image, and try again.", "Error Renaming File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					} else if (ee.Message.IndexOf("already exists") > 0) {
						MpeLog.Warn("An image file with the name [" + e.Label + "] already exists.");
						MessageBox.Show(this, "An image file with the name \"" + e.Label + "\" already exists.", "Error Renaming File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					} else {
						MpeLog.Error(ee);
						MessageBox.Show(this, ee.Message, "Error Renaming File", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
				e.CancelEdit = true;
				e.Node.EndEdit(true);
			} else if (e.Node.Parent == screenNode) {
				if (e.Label.ToLower().EndsWith(".xml") == false) {
					MessageBox.Show(this, "You cannot change a screen file's extension.", "Error Renaming File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					e.CancelEdit = true;
					e.Node.BeginEdit();
					return;
				}
				try {
					MpeScreenInfo info = Parser.RenameScreenFile(e.Node.Text, e.Label);
					e.Node.EndEdit(false);
					e.Node.Tag = info;
					MpeLog.Info("Renamed screen file from [" + e.Node.Text + "] to [" + e.Label + "]");
					skinTree.LabelEdit = false;
					return;
				} catch (Exception ee) {
					MpeLog.Debug(ee);
					if (ee.Message.IndexOf("being used by another process") > 0) {
						MpeLog.Warn("The screen file cannot be renamed because it is locked by another process.");
						MessageBox.Show(this, "The screen cannot be renamed because it is locked by another process.\n\nIf the screen is currently open inside the Media Portal Editor, please close the screen, and try again.", "Error Renaming File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					} else if (ee.Message.IndexOf("already exists") > 0) {
						MpeLog.Warn("A screen file with the name [" + e.Label + "] already exists.");
						MessageBox.Show(this, "A screen file with the name \"" + e.Label + "\" already exists.", "Error Renaming File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					} else {
						MpeLog.Error(ee);
						MessageBox.Show(this, ee.Message, "Error Renaming File", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
				e.CancelEdit = true;
				e.Node.EndEdit(true);
			} else if (e.Node.Parent == fontNode) {
				try {
					Parser.RenameFont(e.Node.Text, e.Label);
					e.Node.EndEdit(false);
					e.Node.Tag = Parser.GetFont(e.Label);
					MpeLog.Info("Renamed font from [" + e.Node.Text + "] to [" + e.Label + "]");
					skinTree.LabelEdit = false;
					return;
				} catch (Exception ee) {
					MpeLog.Debug(ee);
					if (ee.Message.IndexOf("already exists") > 0) {
						MpeLog.Warn("A font with the name [" + e.Label + "] already exists.");
						MessageBox.Show(this, "A font with the name \"" + e.Label + "\" already exists.", "Error Renaming File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					} else {
						MpeLog.Error(ee);
						MessageBox.Show(this, ee.Message, "Error Renaming Font", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
				e.CancelEdit = true;
				e.Node.EndEdit(true);
			}
			skinTree.LabelEdit = false;
		}
		protected void OnMenuSelection(MenuCommand c) {
			if (c == null)
				return;
			if (selectedNode == null || selectedNode.Parent == null)
				return;
			switch(c.Text) {
				case "Delete":
					if (mpe.IsResourceOpen(selectedNode.Text)) {
						MpeLog.Warn("The resource file cannot be deleted because it is currently being editted.");
						MessageBox.Show(this,"The resource file cannot be deleted because it is currently being editted.", "Error Deleting File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						return;
					}
					if (selectedNode.Parent == imageNode) {
						DialogResult result = MessageBox.Show(this, "Are you sure you want to permanently delete the selected image?\n\n" + selectedNode.Text,"Delete Confirmation",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
						if (result == DialogResult.Yes) {
							try {
								Parser.DeleteImageFile(selectedNode.Text);
								MpeLog.Info("Deleted image file [" + selectedNode.Text + "]");
							} catch (Exception ee) {
								MpeLog.Debug(ee);
								MpeLog.Error(ee);
							} finally {
								LoadImages();
							}
						}
					} else if (selectedNode.Parent == screenNode) {
						DialogResult result = MessageBox.Show(this, "Are you sure you want to permanently delete the selected screen?\n\n" + selectedNode.Text,"Delete Confirmation",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
						if (result == DialogResult.Yes) {
							try {
								Parser.DeleteScreenFile(selectedNode.Text);
								MpeLog.Info("Deleted screen file [" + selectedNode.Text + "]");
							} catch (Exception ee) {
								MpeLog.Debug(ee);
								MpeLog.Error(ee);
							} finally {
								LoadScreens();
							}
						}
					} else if (selectedNode.Parent == fontNode) {
						DialogResult result = MessageBox.Show(this, "Are you sure you want to permanently delete the selected font?\n\n" + selectedNode.Text,"Delete Confirmation",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
						if (result == DialogResult.Yes) {
							try {
								Parser.DeleteFont(selectedNode.Text);
								MpeLog.Info("Deleted font [" + selectedNode.Text + "]");
							} catch (Exception ee) {
								MpeLog.Debug(ee);
								MpeLog.Error(ee);
							} finally {
								LoadFonts();
							}
						}
					}
					break;
				case "Rename":
					if (selectedNode.Parent == imageNode || selectedNode.Parent == screenNode || selectedNode.Parent == fontNode) {
						skinTree.SelectedNode = selectedNode;
						skinTree.LabelEdit = true;
						if (selectedNode.IsEditing == false) {
							selectedNode.BeginEdit();
						}	
					} 
					break;
				case "Add Existing...":
					if (selectedNode == imageNode) {
						openFileDialog.Title = "Add Existing Image";
						openFileDialog.Filter = "Image Files (*.png;*.jpg;*.gif;*.bmp)|*.PNG;*.JPG;*.GIF;*.BMP";
						DialogResult result = openFileDialog.ShowDialog(this);
						if (result == DialogResult.OK) {
							try {
								FileInfo newImageFile = Parser.AddImageFile(new FileInfo(openFileDialog.FileName));
								LoadImages();
								for (int i = 0; i < imageNode.Nodes.Count; i++) {
									if (imageNode.Nodes[i].Text.Equals(newImageFile.Name)) {
										selectedNode = imageNode.Nodes[i];
										skinTree.SelectedNode = imageNode.Nodes[i];
										skinTree.LabelEdit = true;
										selectedNode.BeginEdit();
										break;
									}
								}
								MpeLog.Info("Added image file [" + newImageFile.FullName + "]");
							} catch (Exception ee) {
								MpeLog.Debug(ee);
								MpeLog.Error(ee);
							} 
						}
					} else if (selectedNode == screenNode) {
						openFileDialog.Title = "Add Existing Screen";
						openFileDialog.Filter = "Screen Files (*.xml)|*.XML";
						DialogResult result = openFileDialog.ShowDialog(this);
						if (result == DialogResult.OK) {
							try {
								FileInfo newScreenFile = Parser.AddScreenFile(new FileInfo(openFileDialog.FileName));
								LoadScreens();
								for (int i = 0; i < screenNode.Nodes.Count; i++) {
									if (screenNode.Nodes[i].Text.Equals(newScreenFile.Name)) {
										selectedNode = screenNode.Nodes[i];
										skinTree.SelectedNode = screenNode.Nodes[i];
										skinTree.LabelEdit = true;
										selectedNode.BeginEdit();
										break;
									}
								}
								MpeLog.Info("Added screen file [" + openFileDialog.FileName + "]");
							} catch (Exception ee) {
								MpeLog.Debug(ee);
								MpeLog.Error(ee);
							} 
						}
					}
					break;
				case "Add New...":
					if (selectedNode == screenNode) {
						try {
							FileInfo f = Parser.AddScreenFile();
							MpeLog.Info("Added new screen file [" + f.Name + "]");
							LoadScreens();
							for (int i = 0; i < screenNode.Nodes.Count; i++) {
								if (screenNode.Nodes[i].Text.Equals(f.Name)) {
									skinTree.SelectedNode = screenNode.Nodes[i];
									selectedNode = screenNode.Nodes[i];
									skinTree.LabelEdit = true;
									selectedNode.BeginEdit();
									return;
								}
							}
						} catch (Exception ee) {
							MpeLog.Debug(ee);
							MpeLog.Error(ee);
						} 
					} else if (selectedNode == fontNode) {
						try {
							string s = Parser.AddFont();
							MpeLog.Info("Added new font [" + s + "]");
							LoadFonts();
							for (int i = 0; i < fontNode.Nodes.Count; i++) {
								if (fontNode.Nodes[i].Text.Equals(s)) {
									skinTree.SelectedNode = fontNode.Nodes[i];
									selectedNode = fontNode.Nodes[i];
									skinTree.LabelEdit = true;
									selectedNode.BeginEdit();
									return;
								}
							}
						} catch (Exception ee) {
							MpeLog.Debug(ee);
							MpeLog.Error(ee);
						}
					}
					break;
				case "Window":
					if (mpe.IsResourceOpen(selectedNode.Text)) {
						MpeLog.Warn("The screen type cannot be modified because it is currently being editted.");
						MessageBox.Show(this,"The screen type cannot be modified because it is currently being editted..", "Error Modifying Screen Type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					} else {
						if (DialogResult.Yes == MessageBox.Show(this, "Are you sure you want to change this screen's type?","Screen Type Confirmation",MessageBoxButtons.YesNo, MessageBoxIcon.Question)) {
							MpeScreenInfo info = Parser.ModifyScreenType(selectedNode.Text, MpeScreenType.Window);
							selectedNode.Tag = info;
							selectedNode.ImageIndex = 6;
							selectedNode.SelectedImageIndex = 6;
						}
					}
					break;
				case "Dialog":
					if (mpe.IsResourceOpen(selectedNode.Text)) {
						MpeLog.Warn("The screen type cannot be modified because it is currently being editted.");
						MessageBox.Show(this,"The screen type cannot be modified because it is currently being editted..", "Error Modifying Screen Type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					} else {
						if (DialogResult.Yes == MessageBox.Show(this, "Are you sure you want to change this screen's type?","Screen Type Confirmation",MessageBoxButtons.YesNo, MessageBoxIcon.Question)) {
							MpeScreenInfo info = Parser.ModifyScreenType(selectedNode.Text, MpeScreenType.Dialog);
							selectedNode.Tag = info;
							selectedNode.ImageIndex = 7;
							selectedNode.SelectedImageIndex = 7;
						}
					}
					break;
				case "OnScreenDisplay":
					if (mpe.IsResourceOpen(selectedNode.Text)) {
						MpeLog.Warn("The screen type cannot be modified because it is currently being editted.");
						MessageBox.Show(this,"The screen type cannot be modified because it is currently being editted..", "Error Modifying Screen Type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					} else {
						if (DialogResult.Yes == MessageBox.Show(this, "Are you sure you want to change this screen's type?","Screen Type Confirmation",MessageBoxButtons.YesNo, MessageBoxIcon.Question)) {
							MpeScreenInfo info = Parser.ModifyScreenType(selectedNode.Text, MpeScreenType.OnScreenDisplay);
							selectedNode.Tag = info;
							selectedNode.ImageIndex = 8;
							selectedNode.SelectedImageIndex = 8;
						}
					}
					break;
			}
		}
		private void OnMouseDown(object sender, System.Windows.Forms.MouseEventArgs e) {
			selectedNode = skinTree.GetNodeAt(e.X,e.Y);
		}
		private void OnMouseUp(object sender, System.Windows.Forms.MouseEventArgs e) {
			if (e.Button == MouseButtons.Right) {
				if (UpdateContextMenu()) {
					OnMenuSelection(contextMenu.TrackPopup(skinTree.PointToScreen(new Point(e.X, e.Y))));
				}
			}
		}
		#endregion

		#region Component Designer Generated Code
		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing ) {
			if( disposing ) {
				if(components != null) {
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MpeExplorer));
			this.skinTreePanel = new System.Windows.Forms.Panel();
			this.skinTree = new System.Windows.Forms.TreeView();
			this.treeImageList = new System.Windows.Forms.ImageList(this.components);
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.skinTreePanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// skinTreePanel
			// 
			this.skinTreePanel.BackColor = System.Drawing.SystemColors.AppWorkspace;
			this.skinTreePanel.Controls.Add(this.skinTree);
			this.skinTreePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.skinTreePanel.DockPadding.All = 1;
			this.skinTreePanel.Location = new System.Drawing.Point(0, 0);
			this.skinTreePanel.Name = "skinTreePanel";
			this.skinTreePanel.Size = new System.Drawing.Size(312, 368);
			this.skinTreePanel.TabIndex = 0;
			// 
			// skinTree
			// 
			this.skinTree.AllowDrop = true;
			this.skinTree.BackColor = System.Drawing.SystemColors.Window;
			this.skinTree.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.skinTree.Dock = System.Windows.Forms.DockStyle.Fill;
			this.skinTree.ForeColor = System.Drawing.SystemColors.WindowText;
			this.skinTree.HideSelection = false;
			this.skinTree.ImageList = this.treeImageList;
			this.skinTree.Location = new System.Drawing.Point(1, 1);
			this.skinTree.Name = "skinTree";
			this.skinTree.ShowLines = false;
			this.skinTree.Size = new System.Drawing.Size(310, 366);
			this.skinTree.Sorted = true;
			this.skinTree.TabIndex = 0;
			this.skinTree.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnMouseDown);
			this.skinTree.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnMouseUp);
			this.skinTree.DoubleClick += new System.EventHandler(this.OnDoubleClick);
			this.skinTree.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.OnAfterLabelEdit);
			this.skinTree.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.OnItemDrag);
			this.skinTree.BeforeLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.OnBeforeLabelEdit);
			// 
			// treeImageList
			// 
			this.treeImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
			this.treeImageList.ImageSize = new System.Drawing.Size(16, 16);
			this.treeImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("treeImageList.ImageStream")));
			this.treeImageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// MpeExplorer
			// 
			this.Controls.Add(this.skinTreePanel);
			this.Name = "MpeExplorer";
			this.Size = new System.Drawing.Size(312, 368);
			this.skinTreePanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}

	#region Exceptions
	public class MpeExplorerException : Exception {
		public MpeExplorerException(string message) : base(message) {
			//
		}
	}
	#endregion
}
