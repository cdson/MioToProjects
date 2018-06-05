/*
 * Created by SharpDevelop.
 * User: cds
 * Date: 3/22/2017
 * Time: 9:27 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Text;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace MIOToProjects
{
	public class Album
    {
        public string Name;
        public string Artist;
        public List<ExtraInfo> Details;
        public string Url;
        public List<Song> Songs;
        public bool IsDownloaded;

        public Album()
        {
            Details = new List<ExtraInfo>();
            Songs = new List<Song>();
        }

        public override string ToString()
        {
            if (Details.Count > 0)
            {
                return string.Format(@"[ALBUM][Name: {0}][Artist: {1}]][{2}]{3}[Songs: {4}{5}]{6}",
                                    Name, Artist, string.Join(" ,", Details.Select(p => p.Key + " : " + p.Value).ToArray()), Environment.NewLine,
                                    Environment.NewLine, string.Join(" ,", Songs.Select(p => p.ToString()).ToArray()), Environment.NewLine);
            }
            else
            {
                return string.Format(@"[ALBUM][Name: {0}][Artist: {1}]{2}[Songs: {3}{4}]{5}",
                                    Name, Artist, Environment.NewLine, Environment.NewLine,
                                    string.Join(" ,", Songs.Select(p => p.ToString()).ToArray()), Environment.NewLine);
            }
        }
    }

    public class Song
    {
        public string TrackFileName;
        public string TrackArtist;
        public string TrackAlbum;
        public string TrackName;
        public string TrackDiscNumber;
        public string TrackNumber;
        public string TrackGenre;
        
        
        public string TrackUrl;
        public List<ExtraInfo> Details;
        
        public bool IsDownloaded;

        public Song()
        {
            Details = new List<ExtraInfo>();
        }

        public override string ToString()
        {
//            if (Details.Count > 0)
//            {
//                return string.Format("[Album: {0}][Artist: {1}][Disc: {2}][File: {3}][Genre: {4}][Name: {5}][Track: {6}][Url: {7}][{8}]", 
//            	                     TrackAlbum, TrackArtist, TrackDiscNumber, TrackFileName, TrackGenre, TrackName, TrackNumber, TrackUrl,
//            	                     string.Join(" ,", Details.Select(p => p.Key + ":" + p.Value).ToArray()));
//            }
//            else
//            {
//                return string.Format("[Album: {0}][Artist: {1}][Disc: {2}][File: {3}][Genre: {4}][Name: {5}][Track: {6}][Url: {7}]", 
//            	                     TrackAlbum, TrackArtist, TrackDiscNumber, TrackFileName, TrackGenre, TrackName, TrackNumber, TrackUrl);
//            }
            
            if (Details.Count > 0)
            {
                return string.Format("[Artist: {0}][Name: {1}][{2}]{3}", 
            	                     TrackArtist, TrackName, string.Join(" ,", Details.Select(p => p.Key + ":" + p.Value).ToArray()), Environment.NewLine);
            }
            else
            {
                return string.Format("[Artist: {0}][Name: {1}]{2}", 
            	                     TrackArtist, TrackName, Environment.NewLine);
            }
			
//			StringBuilder sb = new StringBuilder();
//			foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(this))
//			{
//			    string name=descriptor.Name;
//			    object value=descriptor.GetValue(this);
//			    sb.AppendFormat("[{0}:{1}]",name,value);
//			}
//			
//			return sb.ToString();
        }
    }

    public class ExtraInfo
    {
        public string Key;
        public string Value;
    }

    public static class Extensions
    {
        public static void SerializeObject(this List<Album> list, string fileName)
        {
        	try
        	{
        		File.Delete(fileName);
        	}
        	catch
        	{
        		
        	}
        		
            var serializer = new XmlSerializer(typeof(List<Album>));
            using (var stream = File.OpenWrite(fileName))
            {
                serializer.Serialize(stream, list);
            }
        }

        public static void Deserialize(this List<Album> list, string fileName)
        {
            var serializer = new XmlSerializer(typeof(List<Album>));
            using (var stream = File.OpenRead(fileName))
            {
                var other = (List<Album>)(serializer.Deserialize(stream));
                list.Clear();
                list.AddRange(other);
            }
        }
    }
    
    public class MyClient : WebClient
    {
        public bool HeadOnly { get; set; }
        private bool _isAudio;
        public bool IsAudio { get { return _isAudio; } }
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest req = base.GetWebRequest(address);
            req.Timeout = 2000;
            if (HeadOnly && req.Method == "GET")
            {
                req.Method = "HEAD";
            }
            return req;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse resp = base.GetWebResponse(request);
            string type = resp.Headers["content-type"];
            _isAudio = type == "audio/mpeg";
            return resp;
        }
    }

	
	/// <summary>
	/// Description of Utils.
	/// </summary>
	public class Utils
	{
		public Utils()
		{
		}

        public static string ReplceIllegalCharacters(string path)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            string newPath = r.Replace(path, "");
            return newPath;
        }
	}
	
	//Copyright (C) Microsoft Corporation.  All rights reserved.


// See the ReadMe.html for additional information
public class ObjectDumper {

    public static void Write(object element)
    {
        Write(element, 0);
    }

    public static void Write(object element, int depth)
    {
        Write(element, depth, Console.Out);
    }

    public static void Write(object element, int depth, TextWriter log)
    {
        ObjectDumper dumper = new ObjectDumper(depth);
        dumper.writer = log;
        dumper.WriteObject(null, element);
    }

    TextWriter writer;
    int pos;
    int level;
    int depth;

    private ObjectDumper(int depth)
    {
        this.depth = depth;
    }

    private void Write(string s)
    {
        if (s != null) {
            writer.Write(s);
            pos += s.Length;
        }
    }

    private void WriteIndent()
    {
        for (int i = 0; i < level; i++) writer.Write("  ");
    }

    private void WriteLine()
    {
        writer.WriteLine();
        pos = 0;
    }

    private void WriteTab()
    {
        Write("  ");
        while (pos % 8 != 0) Write(" ");
    }

    private void WriteObject(string prefix, object element)
    {
        if (element == null || element is ValueType || element is string) {
            WriteIndent();
            Write(prefix);
            WriteValue(element);
            WriteLine();
        }
        else {
            IEnumerable enumerableElement = element as IEnumerable;
            if (enumerableElement != null) {
                foreach (object item in enumerableElement) {
                    if (item is IEnumerable && !(item is string)) {
                        WriteIndent();
                        Write(prefix);
                        Write("...");
                        WriteLine();
                        if (level < depth) {
                            level++;
                            WriteObject(prefix, item);
                            level--;
                        }
                    }
                    else {
                        WriteObject(prefix, item);
                    }
                }
            }
            else {
                MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                WriteIndent();
                Write(prefix);
                bool propWritten = false;
                foreach (MemberInfo m in members) {
                    FieldInfo f = m as FieldInfo;
                    PropertyInfo p = m as PropertyInfo;
                    if (f != null || p != null) {
                        if (propWritten) {
                            WriteTab();
                        }
                        else {
                            propWritten = true;
                        }
                        Write(m.Name);
                        Write("=");
                        Type t = f != null ? f.FieldType : p.PropertyType;
                        if (t.IsValueType || t == typeof(string)) {
                            WriteValue(f != null ? f.GetValue(element) : p.GetValue(element, null));
                        }
                        else {
                            if (typeof(IEnumerable).IsAssignableFrom(t)) {
                                Write("...");
                            }
                            else {
                                Write("{ }");
                            }
                        }
                    }
                }
                if (propWritten) WriteLine();
                if (level < depth) {
                    foreach (MemberInfo m in members) {
                        FieldInfo f = m as FieldInfo;
                        PropertyInfo p = m as PropertyInfo;
                        if (f != null || p != null) {
                            Type t = f != null ? f.FieldType : p.PropertyType;
                            if (!(t.IsValueType || t == typeof(string))) {
                                object value = f != null ? f.GetValue(element) : p.GetValue(element, null);
                                if (value != null) {
                                    level++;
                                    WriteObject(m.Name + ": ", value);
                                    level--;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void WriteValue(object o)
    {
        if (o == null) {
            Write("null");
        }
        else if (o is DateTime) {
            Write(((DateTime)o).ToShortDateString());
        }
        else if (o is ValueType || o is string) {
            Write(o.ToString());
        }
        else if (o is IEnumerable) {
            Write("...");
        }
        else {
            Write("{ }");
        }
    }
}

}
