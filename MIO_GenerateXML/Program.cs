using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ScrapySharp;
using ScrapySharp.Network;
using HtmlAgilityPack;
using System.IO;
using System.Web;
using System.Net;
using System.Xml.Serialization;
using NDesk;
using NDesk.Options;

namespace MIOToProjects
{
	
    class Program
    {
    	static void Main(string[] args)
        {
    		bool show_help = false;
    		string url = string.Empty;
    		string xmlFile = string.Empty;

    		var p = new OptionSet () {
    			{ "u|url=", "the {URL} of MIO Page.", v => url = v },
    			{ "x|xml=",  "the MIO xml file name.", v => xmlFile = v },
    			{ "h|help",  "show this message and exit",
    				v => show_help = v != null },
    		};
    		
        	List<string> extra;
        	try 
        	{
        		extra = p.Parse (args);
        	}
        	catch (OptionException e) {
        		Console.Write ("greet: ");
        		Console.WriteLine (e.Message);
        		Console.WriteLine ("Try `greet --help' for more information.");
        		return;
        	}
        	
        	if (show_help) 
        	{
        		ShowHelp (p);
        		return;
        	}
        	
        	string message;
			if (extra.Count > 0) {
				message = string.Join (" ", extra.ToArray ());
				Console.WriteLine ("Malformed message: {0}", message);
				return;
			}
			
        	
        	//char[] letters = "A".ToCharArray();
            char[] letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            List<Album> albums = new List<Album>();
            
            ScrapingBrowser sb = new ScrapingBrowser();
            sb.AllowAutoRedirect = true;
            sb.AllowMetaRedirect = true;

            string mainUrl = url;
            foreach (var letter in letters)
            {
                WebPage pageResult = sb.NavigateToPage(new Uri(mainUrl + letter), HttpVerb.Get);

                var lst = pageResult.Html.SelectNodes(".//a[starts-with(@href,'/album')]");
                int albumCount = lst != null ? lst.Count : 0;

                if (albumCount > 0)
                {
                    foreach (HtmlNode node in pageResult.Html.SelectNodes(".//a[starts-with(@href,'/album')]"))
                    {
                        try
                        {
                            var albumNode = node;
                            string albumName = albumNode.InnerText;

                            string albumUrl = "http://mio.to" + albumNode.GetAttributeValue("href", "");
                            //LogGenerateXML("---------------------------------------------------------------------------------------------");
                            
                            var additionalInfo = node.NextSibling != null ? node.NextSibling.InnerText : string.Empty;
                            var artist = additionalInfo.Contains("Artist:") || additionalInfo.Contains("Artists:") ? additionalInfo.Split(':')[1] : string.Empty;
                            artist = artist.TrimStart(' ');

                            pageResult = sb.NavigateToPage(new Uri(albumUrl));

                            string albumName_1 = pageResult.Html.SelectSingleNode("//h1[@class='title']").InnerText;

                            albumName = string.Compare(albumName, albumName_1, true) == 0 ? albumName : albumName_1;
                            albumName = HttpUtility.HtmlDecode(Utils.ReplceIllegalCharacters(albumName));

                            var albumId = pageResult.Html.SelectSingleNode("//div[@id='album-id']").InnerText;

                            var album = new Album() { Name = albumName, Artist = artist, Url = albumUrl };

                            var detailsNode = pageResult.Html.SelectSingleNode("//div[@class='details']");
                            if (detailsNode != null)
                            {
                                var detailsNodeList = detailsNode.SelectNodes(".//dt");
                                if (detailsNodeList != null)
                                {
                                    foreach (var dtNode in detailsNodeList)
                                    {
                                        if (!string.IsNullOrEmpty(dtNode.InnerText) && !string.IsNullOrEmpty(dtNode.NextSibling.FirstChild.InnerText))
                                        {
                                            album.Details.Add(new ExtraInfo() { Key = dtNode.InnerText, Value = dtNode.NextSibling.FirstChild.InnerText });
                                        }
                                    }
                                }
                            }

                            //LogGenerateXML(string.Format("[ALBUM: {0}][ARTIST: {1}][URL: {2}]", albumName, artist, albumUrl));

                            int i = 1;
                            
                            //http://media-images.mio.to/by_artist/S/Shobha%20Gurtu/At%20Her%20Creative%20Best%20%282011%29/Art-350.jpg
                            HtmlNode albumArtNode = pageResult.Html.SelectSingleNode(".//img[starts-with(@src,'http://media-images.mio.to/')]");
                            string songUrl_temp = string.Empty;
                            if(albumArtNode != null)
                            {
                            	string src = albumArtNode.GetAttributeValue("src", "");
                            	songUrl_temp = src.Replace("http://media-images.mio.to", "http://media-audio.mio.to");
                            	songUrl_temp = songUrl_temp.Remove(songUrl_temp.LastIndexOf('/') + 1);
                            }
                            
                            foreach (HtmlNode nodeSong in pageResult.Html.SelectNodes(".//tr[@album_id='"+ albumId +"']"))
                            {
                                try
                                {
                                    //string songFileName_temp = nodeSong.Attributes["track_name_s"] != null ? nodeSong.Attributes["track_name_s"].Value : nodeSong.Attributes["track_name"].Value;
                                    string songFileName = nodeSong.Attributes["track_name_s"] != null ? nodeSong.Attributes["track_name_s"].Value : nodeSong.Attributes["track_name"].Value; // Uri.EscapeDataString(Utils.ReplceIllegalCharacters(songFileName_temp));
                                    songFileName = "1_" + i.ToString() + " - " + songFileName + "-vbr-V5.mp3";

                                    //string songUrl = "http://media-audio.mio.to/by_artist/" + albumId[0] + "/" + albumId + "/" + songName;
                                    string songUrl = HttpUtility.HtmlEncode(HttpUtility.UrlPathEncode(songUrl_temp + Uri.EscapeDataString(HttpUtility.HtmlDecode(songFileName))));
                                    	
                                    
                                    using (var client = new MyClient())
                                    {
                                        client.HeadOnly = true;
                                        client.DownloadString(songUrl);
                                        //if (CheckIfFileExistsAtUrl(songUrl))
                                        //if(client.IsAudio)
                                        {

                                            var song = new Song()
                                                        {
                                            	TrackAlbum = HttpUtility.HtmlDecode(nodeSong.GetAttributeValue("track_album","")),
                                            	TrackArtist = HttpUtility.HtmlDecode(nodeSong.GetAttributeValue("track_artist","")),
                                            	TrackDiscNumber = HttpUtility.HtmlDecode(nodeSong.GetAttributeValue("disc_number", "")),
                                            	TrackFileName = songFileName,
                                            	TrackGenre = HttpUtility.HtmlDecode(nodeSong.GetAttributeValue("genre","")),
                                            	TrackName = HttpUtility.HtmlDecode(nodeSong.GetAttributeValue("track_name", "")),
                                            	TrackNumber = HttpUtility.HtmlDecode(nodeSong.GetAttributeValue("track_number","")),
                                            	TrackUrl = songUrl,
                                                        };

                                            var groupNode = pageResult.Html.SelectSingleNode(".//div[@class='group']");
                                            if (groupNode != null)
                                            {
                                                var detailsNodeList = groupNode.SelectNodes(".//dt");
                                                if (detailsNodeList != null)
                                                {
                                                    foreach (var dtNode in detailsNodeList)
                                                    {
                                                        if (!string.IsNullOrEmpty(dtNode.InnerText) && !string.IsNullOrEmpty(dtNode.NextSibling.FirstChild.InnerText))
                                                        {
                                                            song.Details.Add(new ExtraInfo() { Key = dtNode.InnerText, Value = dtNode.NextSibling.FirstChild.InnerText });
                                                        }
                                                    }
                                                }
                                            }

                                            album.Songs.Add(song);
                                            
                                        }
                                        //else
                                        //{
                                        //    LogGenerateXML(String.Format("[INVALID_AUDIO_FILE][SONG_NAME: {0}][SONG_URL: {1}]", songName, songUrl));
                                        //}
                                    }

                                    i++;

                                }
                                catch (Exception ex)
                                {

                                }
                            }
                            
                            //ObjectDumper.Write(album, 4);
                            LogGenerateXML(album.ToString());

                            if(album.Songs.Count > 0)
                            {
                            	albums.Add(album);
                            }
                        }
                        catch(Exception ex)
                        {

                        }
                    }
                }
                
            }
            
            //XElement xml = new XElement("Albums",
            //                    from album in albums
            //                    select new XElement("Album",
            //                        new XAttribute("Name", album.Name),
            //                        new XAttribute("Artist", album.Artist),
            //                        new XAttribute("Url", album.Url),
            //                        new XElement("Details",
            //                                     from dt_album in album.Details
            //                                     select new XElement(dt_album.Key.Replace(" ",""), dt_album.Value)),
            //                        new XElement("Songs", 
            //                                     from song in album.Songs
            //                                     select new XElement("Song",
            //                                                         new XAttribute("Name", song.Name),
            //                                                         new XAttribute("Url", song.Url),
            //                                                         new XElement("Details", 
            //                                                                      from dt_song in song.Details
            //                                                                      select new XElement(dt_song.Key.Replace(" ",""), dt_song.Value))
            //                                                        )
            //                                    )
            //                       )
            //                   );
            
            //xml.Save("MIO_Classical_Hindustani_Vocal.xml");

            albums.SerializeObject(xmlFile);
            
            Console.WriteLine("DONE !!");

            Console.ReadLine();
        }
    	
    	static void ShowHelp (OptionSet p)
    	{
    		Console.WriteLine ("Usage: MIO_GenerateXML [OPTIONS]");
    		Console.WriteLine ();
    		Console.WriteLine ("Options:");
    		p.WriteOptionDescriptions (Console.Out);
    	}
        
        private static bool CheckIfFileExistsAtUrl(string url)
        {
        	bool res = false;
        	HttpWebResponse response = null;
        	var request = (HttpWebRequest)WebRequest.Create(url);
        	request.Method = "HEAD";


        	try
        	{
        		response = (HttpWebResponse)request.GetResponse();
        		res = response.StatusCode == HttpStatusCode.OK;
        	}
        	catch (WebException ex)
        	{
        		/* A WebException will be thrown if the status of the response is not `200 OK` */
        	}
        	finally
        	{
        		// Don't forget to close your response.
        		if (response != null)
        		{
        			response.Close();
        		}
        	}
        	
        	return res;
        }
        
        private static void LogGenerateXML(string message)
        {
        	Console.WriteLine(message);
        	
        	using(StreamWriter sw = new StreamWriter("MIO_GenerateXML.log", true))
        	{
        		sw.WriteLine(message);
        	}
        }
    }
    
    
 }
    
