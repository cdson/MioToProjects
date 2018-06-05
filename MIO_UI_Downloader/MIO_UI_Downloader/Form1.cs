#define TEST

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace MIOToProjects
{
    public partial class Form1 : Form
    {
    	List<Album> albums = new List<Album>();
    	
    	Dictionary<string, string> songFiles = new Dictionary<string, string>();
    		
   		public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        	textBox1.Text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); //Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ofd.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            if(ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox2.Text = ofd.FileName;
                
                albums.Deserialize(textBox2.Text);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                // SECTION 1. Create a DOM Document and load the XML data into it.
                XmlDocument dom = new XmlDocument();
                dom.Load(textBox2.Text);

                // SECTION 2. Initialize the TreeView control.
                treeView1.Nodes.Clear();
                treeView1.Nodes.Add(new TreeNode(dom.DocumentElement.Name));
                TreeNode tNode = new TreeNode();
                tNode = treeView1.Nodes[0];

                // SECTION 3. Populate the TreeView with the DOM nodes.
                //AddNode(dom.DocumentElement, tNode);
                AddChildNodes(dom.DocumentElement, tNode);
                treeView1.Nodes[0].Text += treeView1.Nodes[0].Text + "[Total: " + tNode.Nodes.Count + "]";
                treeView1.ExpandAll();
            }
            catch (XmlException xmlEx)
            {
                MessageBox.Show(xmlEx.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
        private void AddChildNodes(XmlNode inXmlNode, TreeNode inTreeNode)
        {
        	foreach(XmlNode albumNode in inXmlNode.SelectNodes("//Album"))
        	{
                string albumTreeNodeText = string.Format("[AlbumName: {0}][AlbumArtist: {1}]", albumNode.SelectSingleNode("./Name").InnerText, albumNode.SelectSingleNode("./Artist").InnerText);
        		
        		TreeNode albumTreeNode = new TreeNode(albumTreeNodeText);
        		inTreeNode.Nodes.Add(albumTreeNode);

                var albumInfo = albums.Find(p => string.Compare(p.Name, albumNode.SelectSingleNode("./Name").InnerText, true) == 0 &&
                                                 string.Compare(p.Artist, albumNode.SelectSingleNode("./Artist").InnerText) == 0);

                if (albumInfo != null)
                {
                    albumTreeNode.Tag = albumInfo;
                    albumTreeNode.Checked = albumInfo.IsDownloaded;
                }
        		
        		foreach(XmlNode songNode in albumNode.SelectNodes(".//Song"))
        		{
        			var song = albumInfo.Songs.Find(p => string.Compare(p.TrackName, songNode.SelectSingleNode("./TrackName").InnerText, true) == 0);
        			
        			if(song != null)
        			{
        				string songTreeNodeText = string.Format("[TrackName: {0}][TrackArtist: {1}]", songNode.SelectSingleNode("./TrackName").InnerText, songNode.SelectSingleNode("./TrackArtist").InnerText);
        				TreeNode songTreeNode = new TreeNode(songTreeNodeText);
        				
        				if(albumInfo.IsDownloaded && !song.IsDownloaded)
        				{
        					song.IsDownloaded = albumInfo.IsDownloaded;
        				}
        				
        				songTreeNode.Tag = song;
        				songTreeNode.Checked = song.IsDownloaded;
        				albumTreeNode.Nodes.Add(songTreeNode);
        			}
        		}

                //textBox3.AppendText(albumInfo.ToString());
#if TEST
                LogAlbumInfo(albumInfo.ToString());
#endif
        	}


        }
        
        

        private void LogAlbumInfo(string message)
        {
            textBox3.AppendText(message);

            using(StreamWriter sw = new StreamWriter(Path.GetFileNameWithoutExtension(textBox2.Text) + ".txt", true))
            {
                sw.WriteLine(message);
                sw.WriteLine("---------------------------------------------------------------------------");
            }
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // The code only executes if the user caused the checked state to change.
            if (e.Action != TreeViewAction.Unknown)
            {
            	var album = e.Node.Tag as Album;
                if(album != null)
                {
                	album.IsDownloaded = e.Node.Checked;
                }
                
                var song = e.Node.Tag as Song;
                if(song != null)
                {
                	song.IsDownloaded = e.Node.Checked;
                }
                
                if (e.Node.Nodes.Count > 0)
                {
                    /* Calls the CheckAllChildNodes method, passing in the current 
                    Checked value of the TreeNode whose checked state changed. */
                    this.CheckAllChildNodes(e.Node, e.Node.Checked);
                }
            }
        }

        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
        	foreach (TreeNode node in treeNode.Nodes)
            {
                node.Checked = nodeChecked;
                
                var album = node.Tag as Album;
                if(album != null)
                {
                	album.IsDownloaded = nodeChecked;
                }
                
                var song = node.Tag as Song;
                if(song != null)
                {
                	song.IsDownloaded = nodeChecked;
                }
                
                if (node.Nodes.Count > 0)
                {
                    // If the current node has child nodes, call the CheckAllChildsNodes method recursively.
                    this.CheckAllChildNodes(node, nodeChecked);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
        	DirectoryInfo dirGenre = null;
            try
            {
                dirGenre = Directory.CreateDirectory(Path.GetFileNameWithoutExtension(textBox2.Text));
            }
            catch(Exception ex)
            {
                LogAlbumInfo(Environment.NewLine);
                LogAlbumInfo("[FAILED_TO_CREATE_ROOT_DIR]" + ex.Message);
                return;
            }

            foreach(var albumInfo in albums)
            {
                if(albumInfo.IsDownloaded)
                {
                    
                    string albumDirPath = Utils.ReplceIllegalCharacters(albumInfo.Name);
                    
                    DirectoryInfo albumDir = null;
                    try
                    {
                    	albumDir = Directory.CreateDirectory(Path.Combine(dirGenre.FullName, albumDirPath));
                    }
                    catch(Exception ex)
                    {
                    	LogAlbumInfo(Environment.NewLine);
                    	LogAlbumInfo("[INVALID_ALBUM_DIR]" + ex.Message);
                    }

                    uint songCount = 1;
                    foreach(var song in albumInfo.Songs)
                    {
                    	try
                    	{
                    		if(song.IsDownloaded)
                    		{
                    			if(albumDir == null)
                    			{
                    				LogAlbumInfo(Environment.NewLine);
                    				LogAlbumInfo("[ALBUM_DIR_INVALID_NAME]" + albumInfo.Name);
                    			}
                    			
                    			string songPath = Path.Combine(albumDir.FullName, Utils.ReplceIllegalCharacters((song.TrackFileName)));

                    			if (!File.Exists(songPath) && !string.IsNullOrEmpty(song.TrackUrl))
                    			{
                    				#if TEST
                    				LogAlbumInfo(Environment.NewLine);
                    				LogAlbumInfo("DOWNLOADING : " + songPath);
                    				#endif
                    				using (var dc = new WebClient())
                    				{
                    					dc.DownloadFile(song.TrackUrl, songPath);

                    					//ConvertToMP3(songPath, Path.GetFileNameWithoutExtension(songPath) + "_transcode" + Path.GetExtension(songPath));
                    				}
                    				#if TEST
                    				LogAlbumInfo(Environment.NewLine);
                    				LogAlbumInfo("DOWNLOAD SUCCESSFUL : " + songPath);
                    				LogAlbumInfo(Environment.NewLine);
                    				#endif

                    				try
                    				{
                    					if(IsDuplicateFile(songPath))
                    					{
                    						LogAlbumInfo(Environment.NewLine);
                    						LogAlbumInfo("[SONG_DUPLICATE_FILE: " + songPath + "]");
                    						continue;
                    					}
                    				}
                    				catch(Exception ex)
                    				{
                    					LogAlbumInfo(Environment.NewLine);
                    					LogAlbumInfo(ex.Message);
                    				}
                    				
                    				
                    				try
                    				{
                    					var tlf = TagLib.File.Create(songPath);
                    					tlf.Tag.Album = song.TrackAlbum; //albumInfo.Name;

                    					List<string> artists = new List<string>();
                    					if (!string.IsNullOrEmpty(albumInfo.Artist))
                    					{
                    						artists.Add(albumInfo.Artist);
                    					}

                    					foreach (var songArtist in song.Details.Where(p => string.Compare(p.Key, "artist", true) == 0))
                    					{
                    						artists.Add(songArtist.Value);
                    					}

                    					tlf.Tag.AlbumArtists = new string[] { song.TrackArtist }; //artists.ToArray();
                    					tlf.Tag.TrackCount = (uint)albumInfo.Songs.Count;
                    					tlf.Tag.Track = Convert.ToUInt32(song.TrackNumber); //songCount++;
                    					tlf.Tag.Disc = Convert.ToUInt32(song.TrackDiscNumber);


                    					string comment = string.Empty;
                    					if (albumInfo.Details.Count > 0)
                    					{
                    						comment = string.Format("[ALBUM_INFO][{0}]", albumInfo.Details.Select(p => p.Key + ":" + p.Value).ToArray());
                    					}

                    					if (song.Details.Count > 0)
                    					{
                    						comment += string.Format("[SONG_INFO][{0}]", string.Join(" ,", song.Details.Select(p => p.Key + ":" + p.Value).ToArray()));
                    					}
                    					tlf.Tag.Comment = comment;

                    					tlf.Tag.Title = song.TrackName;
                    					tlf.Tag.Genres = new string[] { song.TrackGenre }; //new string[] { Path.GetFileNameWithoutExtension(textBox2.Text) };

                    					tlf.Save();
                    				}
                    				catch(Exception ex)
                    				{
                    					LogAlbumInfo(Environment.NewLine);
                    					//LogAlbumInfo(string.Format("[INVALID_AUDIO_FILE][AlbumUrl: {0}][SongName: {1}][{2}]", albumInfo.Url, song.TrackName,  ex.Message));
                    					LogAlbumInfo(string.Format("[INVALID_AUDIO_FILE][SongUrl: {0}]", song.TrackUrl));
                    					
                    					try
                    					{
                    						File.Delete(songPath);
                    					}
                    					catch
                    					{

                    					}
                    				}
                    				
                    			}
                    		}
                    		
                    	}
                    	catch(Exception ex)
                    	{
                    		LogAlbumInfo(Environment.NewLine);
                    		LogAlbumInfo(ex.Message);
                    	}
                    	
                    }

                    if(albumDir.GetFiles().Count() == 0)
                    {
                        albumDir.Delete(true);
                        LogAlbumInfo("[DELETED_EMPTY_DIR][PATH: " + albumDir.FullName + "]");
                    }

                    albumInfo.IsDownloaded = true;
                }
            }

            albums.SerializeObject(Path.GetFileName(textBox2.Text));
        }
        
        private bool IsDuplicateFile(string filePath)
        {
        	string sha1sum = string.Empty;
        	
        	using (FileStream fs = new FileStream(filePath, FileMode.Open))
        	using (BufferedStream bs = new BufferedStream(fs))
        	{
        		using (SHA1Managed sha1 = new SHA1Managed())
        		{
        			byte[] hash = sha1.ComputeHash(bs);
        			StringBuilder formatted = new StringBuilder(2 * hash.Length);
        			foreach (byte b in hash)
        			{
        				formatted.AppendFormat("{0:X2}", b);
        			}
        			
        			sha1sum = formatted.ToString();
        		}
        	}
        	
        	if(songFiles.ContainsKey(sha1sum))
        	{
        		//Duplicate
        		return true;
        	}
        	else
        	{
        		songFiles.Add(sha1sum, filePath);
        		return false;
        	}
        }

        static void ConvertToMP3(string sourceFilename, string targetFilename)
        {
            using (var reader = new NAudio.Wave.AudioFileReader(sourceFilename))
            using (var writer = new NAudio.Lame.LameMP3FileWriter(targetFilename, reader.WaveFormat, NAudio.Lame.LAMEPreset.STANDARD))
            {
                reader.CopyTo(writer);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            albums.SerializeObject(Path.GetFileName(textBox2.Text));
            
            MessageBox.Show("AlbumList XML Saved Successfully !");
        }

        //http://media-audio.mio.to/by_artist/R/Rashid%20Khan%20%28Ustad%29/Albela%20Sajan%20Aayo%20Re%20-%20Ustad%20Rashid%20Khan%20%282007%29/1_1%20-%20Albela%20Sajan%20Aayo%20Re-vbr-V5.mp3
    }
}
