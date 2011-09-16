using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioAlign.Audio.Project;
using System.Diagnostics;
using AudioAlign.Audio.Streams;

namespace AudioAlign.Audio.Matching {
    public class MatchProcessor {

        /// <summary>
        /// Creates a list of all possible track pairs from a track list.
        /// </summary>
        public static List<Tuple<AudioTrack, AudioTrack>> GetTrackPairs(TrackList<AudioTrack> trackList) {
            List<Tuple<AudioTrack, AudioTrack>> pairs = new List<Tuple<AudioTrack, AudioTrack>>();
            for (int x = 0; x < trackList.Count; x++) {
                for (int y = x + 1; y < trackList.Count; y++) {
                    pairs.Add(new Tuple<AudioTrack, AudioTrack>(trackList[x], trackList[y]));
                }
            }
            return pairs;
        }

        /// <summary>
        /// Creates a list of track pairs including their matches from a list ot track pairs and a list of matches.
        /// </summary>
        public static List<Tuple<AudioTrack, AudioTrack, List<Match>>> GetTrackPairsMatches(List<Tuple<AudioTrack, AudioTrack>> trackPairs, IEnumerable<Match> matches) {
            List<Tuple<AudioTrack, AudioTrack, List<Match>>> trackPairMatches = 
                new List<Tuple<AudioTrack, AudioTrack, List<Match>>>();
            foreach (Tuple<AudioTrack, AudioTrack> trackPair in trackPairs) {
                List<Match> pairMatches = new List<Match>();
                foreach (Match match in matches) {
                    if (match.Track1 == trackPair.Item1 && match.Track2 == trackPair.Item2
                        || match.Track2 == trackPair.Item1 && match.Track1 == trackPair.Item2) {
                        pairMatches.Add(match);
                    }
                }
                if (pairMatches.Count > 0) {
                    trackPairMatches.Add(new Tuple<AudioTrack, AudioTrack, List<Match>>(
                        trackPair.Item1, trackPair.Item2, pairMatches));
                }
            }
            return trackPairMatches;
        }

        /// <summary>
        /// Scans a collection of matches and returns a list of all affected audio tracks.
        /// </summary>
        private static List<AudioTrack> GetTracks(List<Match> matches) {
            List<AudioTrack> tracks = new List<AudioTrack>();

            foreach (Match match in matches) {
                if (!tracks.Contains(match.Track1)) {
                    tracks.Add(match.Track1);
                }
                if (!tracks.Contains(match.Track2)) {
                    tracks.Add(match.Track2);
                }
            }

            return tracks;
        }

        /// <summary>
        /// Validates that a collection of matches only belongs to two single distinct tracks.
        /// </summary>
        /// <see cref="GetTracks(List<List>)"/>
        public static void ValidatePair(List<Match> matches) {
            List<AudioTrack> tracks = GetTracks(matches);
            if (tracks.Count != 2) {
                throw new ArgumentException("the collection of matches doesn't belong to a single pair of tracks");
            }
        }

        /// <summary>
        /// Validates a collection of matches so that the matches only belong to a pair of two single tracks,
        /// and that the matches' first track is always one single track, and the matches' second track is always the second
        /// single track.
        /// </summary>
        /// <see cref="GetTracks(List<List>)"/>
        /// <see cref="ValidatePair(List<List>)"/>
        public static void ValidatePairOrder(List<Match> matches) {
            ValidatePair(matches);
            List<AudioTrack> tracks = GetTracks(matches);
            AudioTrack t1 = tracks[0];
            AudioTrack t2 = tracks[1];

            foreach (Match match in matches) {
                if (match.Track1 != t1 || match.Track2 != t2) {
                    throw new Exception("pair order violated");
                }
            }
        }

        /// <summary>
        /// Filters a collection of matches for a pair of tracks according to the specified mode.
        /// </summary>
        /// <returns>a single match, chosen according to the specified match filter mode</returns>
        public static Match Filter(List<Match> matches, MatchFilterMode mode) {
            if (matches.Count == 0) {
                throw new ArgumentException("no matches to filter");
            }
            if (GetTracks(matches).Count != 2) {
                throw new ArgumentException("matches must contain a single pair of affected tracks");
            }

            if (mode == MatchFilterMode.Best) {
                return matches.OrderByDescending(m => m.Similarity).First();
            }
            else if (mode == MatchFilterMode.First) {
                return matches.OrderBy(m => m.Track1Time).First();
            }
            else if (mode == MatchFilterMode.Mid) {
                return matches.OrderBy(m => m.Track1Time).ElementAt(matches.Count() / 2);
            }
            else if (mode == MatchFilterMode.Last) {
                return matches.OrderBy(m => m.Track1Time).Last();
            }
            else {
                throw new NotImplementedException("mode not implemented: " + mode);
            }
        }

        /// <summary>
        /// Filters a collection of matches by applying a sliding window and determining the best match for
        /// each window according to the specified filter mode.
        /// </summary>
        /// <returns>a list of matches containing at least one match</returns>
        public static List<Match> WindowFilter(List<Match> matches, MatchFilterMode mode, TimeSpan windowSize) {
            if (matches.Count == 0) {
                throw new ArgumentException("no matches to filter");
            }
            if (GetTracks(matches).Count != 2) {
                throw new ArgumentException("matches must contain a single pair of affected tracks");
            }

            // sort matches by time
            AudioTrack audioTrack = null;
            foreach (Match match in matches) {
                if (audioTrack == null) {
                    audioTrack = match.Track1;
                }
                else {
                    if (match.Track1 != audioTrack) {
                        match.SwapTracks();
                    }
                }
            }
            matches = new List<Match>(matches.OrderBy(match => match.Track1Time));

            TimeSpan filterWindow = windowSize;
            TimeSpan filterWindowStart = new TimeSpan(0);
            TimeSpan filterWindowEnd = windowSize;
            List<Match> filterWindowMatches = new List<Match>();
            List<Match> filteredWindowMatches = new List<Match>();

            while (filterWindowStart < matches.Last().Track1Time) {
                // get matches belonging to the current window
                foreach (Match match in matches) {
                    if (match.Track1Time >= filterWindowStart && match.Track1Time <= filterWindowEnd) {
                        filterWindowMatches.Add(match);
                    }
                }
                // process current window and switch to next window
                if (filterWindowMatches.Count > 0) {
                    filteredWindowMatches.Add(Filter(filterWindowMatches, mode));
                    filterWindowMatches.Clear();
                }
                filterWindowStart += filterWindow;
                filterWindowEnd += filterWindow;
            }
            return filteredWindowMatches;
        }

        public static void Align(Match match, AudioTrack trackToAdjust) {
            if (match.Track1 == trackToAdjust) {
                match.Track1.Offset = match.Track2.Offset + match.Track2Time - match.Track1Time;
            }
            else if(match.Track2 == trackToAdjust) {
                match.Track2.Offset = match.Track1.Offset + match.Track1Time - match.Track2Time;
            }
            else {
                throw new Exception("the track to adjust doesn't belong to the match");
            }
        }

        public static AudioTrack Align(Match match) {
            if (match.Track1.Offset + match.Track1Time < match.Track2.Offset + match.Track2Time) {
                // move track 1
                Align(match, match.Track1);
                return match.Track1;
            }
            else {
                // move track 2
                Align(match, match.Track2);
                return match.Track2;
            }
        }

        public static AudioTrack TimeWarp(List<Match> matches, AudioTrack trackToWarp) {
            if (matches.Count == 0) {
                throw new ArgumentException("no matches to filter");
            }
            if (GetTracks(matches).Count != 2) {
                throw new ArgumentException("matches must contain a single pair of affected tracks");
            }

            AudioTrack track = trackToWarp;
            AudioProperties trackProperties = track.CreateAudioStream().Properties;
            List<TimeWarp> timeWarps = new List<TimeWarp>();

            if (track == matches[0].Track1) {
                // since we always warp the second track and the requested track to warp is currently the
                // first one, the matches need to be swapped
                foreach(Match match in matches) {
                    match.SwapTracks();
                }
            }

            // calculate time warps from track's matches
            Match m1 = matches[0];
            Match m2 = null;
            for (int i = 1; i < matches.Count; i++) {
                m2 = matches[i];
                TimeSpan targetTime = m1.Track2Time + (m2.Track1Time - m1.Track1Time);
                timeWarps.Add(new TimeWarp() {
                    From = TimeUtil.TimeSpanToBytes(m2.Track2Time, trackProperties),
                    To = TimeUtil.TimeSpanToBytes(targetTime, trackProperties)
                });
                m2.Track2Time = targetTime;
                // TODO alle anderen Matches die Track 2 betreffen anpassen
            }

            // apply time warps to the track
            track.TimeWarps.AddRange(timeWarps);

            return track;
        }

        public static void AlignTracks(List<Tuple<AudioTrack, AudioTrack, List<Match>>> trackPairsMatches, List<Match> allMatches) {
            allMatches = new List<Match>(allMatches); // create a new list for modification purposes
            List<AudioTrack> alignedTracks = new List<AudioTrack>();

            foreach (Tuple<AudioTrack, AudioTrack, List<Match>> trackPairMatches in trackPairsMatches) {
                trackPairMatches.Item1.TimeWarps.Clear();
                trackPairMatches.Item2.TimeWarps.Clear();
            }

            foreach (Tuple<AudioTrack, AudioTrack, List<Match>> trackPairMatches in trackPairsMatches) {
                AudioTrack trackToAlign = alignedTracks.Contains(trackPairMatches.Item2) ?
                    trackPairMatches.Item1 : trackPairMatches.Item2;

                Align(trackPairMatches.Item3[0], trackToAlign);
                Debug.WriteLine("aligned: " + trackToAlign);
                if (trackPairMatches.Item3.Count > 1) {
                    TimeWarp(trackPairMatches.Item3, trackToAlign);
                    Debug.WriteLine("warped: " + trackToAlign);
                    // adjust all other matches related to the currently warped track
                    AudioProperties properties = trackToAlign.CreateAudioStream().Properties;
                    List<Match> adjustedMatches = new List<Match>();
                    foreach (Match match in allMatches) {
                        if (!trackPairMatches.Item3.Contains(match)) {
                            if (match.Track1 == trackToAlign) {
                                match.Track1Time = TimeUtil.BytesToTimeSpan(
                                    trackToAlign.TimeWarps.TranslateSourceToWarpedPosition(
                                    TimeUtil.TimeSpanToBytes(match.Track1Time, properties)), properties);
                                adjustedMatches.Remove(match);
                            }
                            else if (match.Track2 == trackToAlign) {
                                match.Track2Time = TimeUtil.BytesToTimeSpan(
                                    trackToAlign.TimeWarps.TranslateSourceToWarpedPosition(
                                    TimeUtil.TimeSpanToBytes(match.Track2Time, properties)), properties);
                                adjustedMatches.Remove(match);
                            }
                        }
                    }
                    allMatches.RemoveAll(match => adjustedMatches.Contains(match));
                }
                alignedTracks.Add(trackToAlign);
            }
        }

        public static void MoveToStartTime(TrackList<AudioTrack> trackList, TimeSpan startTime) {
            TimeSpan start = trackList.Start;
            TimeSpan delta = startTime - start;
            foreach (AudioTrack audioTrack in trackList) {
                audioTrack.Offset += delta;
            }
        }
    }
}
