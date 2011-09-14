using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Matching;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using AudioAlign.Audio.Streams;

namespace AudioAlign.Audio.Project {
    public class Project {

        private readonly TrackList<AudioTrack> audioTrackList;
        private readonly List<Match> matches;

        private FileInfo projectFile;

        public Project() {
            audioTrackList = new TrackList<AudioTrack>();
            matches = new List<Match>();
        }

        public TrackList<AudioTrack> AudioTracks {
            get { return audioTrackList; }
        }

        public List<Match> Matches {
            get { return matches; }
        }

        public float MasterVolume { get; set; }
        public FileInfo File { get; set; }

        public static void Save(Project project, FileInfo targetFile) {
            if (targetFile.Exists) {
                //targetFile.Delete();
            }
            Stream stream = targetFile.Create();

            XmlTextWriter xml = new XmlTextWriter(stream, null);
            xml.WriteStartElement("project");
            
            // project format version
            xml.WriteStartElement("format");
            xml.WriteValue(1);
            xml.WriteEndElement();

            // audio tracks
            xml.WriteStartElement("audiotracks");
            foreach (AudioTrack track in project.AudioTracks) {
                xml.WriteStartElement("track");

                xml.WriteStartAttribute("file");
                xml.WriteString(track.FileInfo.FullName);
                xml.WriteEndAttribute();

                xml.WriteStartAttribute("name");
                xml.WriteString(track.Name);
                xml.WriteEndAttribute();

                xml.WriteStartAttribute("length");
                xml.WriteString(track.Length.ToString());
                xml.WriteEndAttribute();

                xml.WriteStartAttribute("offset");
                xml.WriteString(track.Offset.ToString());
                xml.WriteEndAttribute();

                xml.WriteStartAttribute("mute");
                xml.WriteValue(track.Mute);
                xml.WriteEndAttribute();

                xml.WriteStartAttribute("solo");
                xml.WriteValue(track.Solo);
                xml.WriteEndAttribute();

                xml.WriteStartAttribute("volume");
                xml.WriteValue(track.Volume);
                xml.WriteEndAttribute();

                xml.WriteStartAttribute("balance");
                xml.WriteValue(track.Balance);
                xml.WriteEndAttribute();

                xml.WriteStartAttribute("invertedphase");
                xml.WriteValue(track.InvertedPhase);
                xml.WriteEndAttribute();

                xml.WriteStartElement("timewarps");
                foreach (TimeWarp warp in track.TimeWarps) {
                    xml.WriteStartElement("timewarp");

                    xml.WriteStartAttribute("from");
                    xml.WriteValue(warp.From);
                    xml.WriteEndAttribute();

                    xml.WriteStartAttribute("to");
                    xml.WriteValue(warp.To);
                    xml.WriteEndAttribute();

                    xml.WriteEndElement();
                }
                xml.WriteEndElement();

                xml.WriteEndElement();
            }
            xml.WriteEndElement();

            // matches
            xml.WriteStartElement("matches");
            foreach (Match match in project.Matches) {
                xml.WriteStartElement("match");

                xml.WriteStartAttribute("track1");
                xml.WriteValue(project.AudioTracks.IndexOf(match.Track1));
                xml.WriteEndAttribute();

                xml.WriteStartAttribute("track1time");
                xml.WriteString(match.Track1Time.ToString());
                xml.WriteEndAttribute();

                xml.WriteStartAttribute("track2");
                xml.WriteValue(project.AudioTracks.IndexOf(match.Track2));
                xml.WriteEndAttribute();

                xml.WriteStartAttribute("track2time");
                xml.WriteString(match.Track2Time.ToString());
                xml.WriteEndAttribute();

                xml.WriteStartAttribute("similarity");
                xml.WriteValue(match.Similarity);
                xml.WriteEndAttribute();

                xml.WriteEndElement();
            }
            xml.WriteEndElement();

            // global settings
            xml.WriteStartElement("mastervolume");
            xml.WriteValue(project.MasterVolume);
            xml.WriteEndElement();

            xml.WriteEndElement();

            xml.Flush();
            xml.Close();
        }

        public static Project Load(FileInfo sourceFile) {
            Project project = new Project();
            Stream stream = sourceFile.OpenRead();
            XmlTextReader xml = new XmlTextReader(stream);

            xml.ReadStartElement("project");

            // project format version
            xml.ReadStartElement("format");
            int formatVersion = xml.ReadContentAsInt();
            if (formatVersion != 1) {
                throw new Exception("invalid project file format");
            }
            xml.ReadEndElement();

            // audio tracks
            if (xml.IsStartElement("audiotracks")) {
                bool empty = xml.IsEmptyElement;
                xml.ReadStartElement("audiotracks");
                if (!empty) {
                    while (xml.IsStartElement("track")) {
                        xml.MoveToAttribute("file");
                        string file = xml.Value;
                        AudioTrack track = new AudioTrack(new FileInfo(file));

                        xml.MoveToAttribute("name");
                        track.Name = xml.Value;

                        xml.MoveToAttribute("length");
                        track.Length = TimeSpan.Parse(xml.Value);

                        xml.MoveToAttribute("offset");
                        track.Offset = TimeSpan.Parse(xml.Value);

                        xml.MoveToAttribute("mute");
                        track.Mute = xml.ReadContentAsBoolean();

                        xml.MoveToAttribute("solo");
                        track.Solo = xml.ReadContentAsBoolean();

                        xml.MoveToAttribute("volume");
                        track.Volume = xml.ReadContentAsFloat();

                        xml.MoveToAttribute("balance");
                        track.Balance = xml.ReadContentAsFloat();

                        xml.MoveToAttribute("invertedphase");
                        track.InvertedPhase = xml.ReadContentAsBoolean();

                        xml.ReadStartElement("track");
                        if (xml.IsStartElement("timewarps")) {
                            empty = xml.IsEmptyElement;
                            xml.ReadStartElement("timewarps");
                            if (!empty) {
                                while (xml.IsStartElement("timewarp")) {
                                    xml.ReadStartElement();

                                    TimeWarp warp = new TimeWarp();

                                    xml.MoveToAttribute("from");
                                    warp.From = xml.ReadContentAsLong();

                                    xml.MoveToAttribute("to");
                                    warp.To = xml.ReadContentAsLong();

                                    //xml.ReadEndElement(); // not necessary since timewarp is an empty element

                                    track.TimeWarps.Add(warp);
                                }
                                xml.ReadEndElement(); // timewarps
                            }
                        }

                        xml.ReadEndElement(); // track
                        project.AudioTracks.Add(track);
                    }
                    xml.ReadEndElement(); // audiotracks
                }
            }

            // matches
            if (xml.IsStartElement("matches")) {
                bool empty = xml.IsEmptyElement;
                xml.ReadStartElement("matches");
                if (!empty) {
                    while (xml.IsStartElement("match")) {
                        
                        Match match = new Match();

                        xml.MoveToAttribute("track1");
                        match.Track1 = project.AudioTracks[xml.ReadContentAsInt()];

                        xml.MoveToAttribute("track1time");
                        match.Track1Time = TimeSpan.Parse(xml.Value);

                        xml.MoveToAttribute("track2");
                        match.Track2 = project.AudioTracks[xml.ReadContentAsInt()];

                        xml.MoveToAttribute("track2time");
                        match.Track2Time = TimeSpan.Parse(xml.Value);

                        xml.MoveToAttribute("similarity");
                        match.Similarity = xml.ReadContentAsFloat();

                        project.Matches.Add(match);
                        xml.ReadStartElement("match");
                    }
                    xml.ReadEndElement(); // matches
                }
            }

            // global settings
            xml.ReadStartElement("mastervolume");
            project.MasterVolume = xml.ReadContentAsFloat();
            xml.ReadEndElement();

            xml.ReadEndElement();

            xml.Close();
            return project;
        }
    }
}
