// 
// Aurio: Audio Processing, Analysis and Retrieval Library
// Copyright (C) 2010-2017  Mario Guggenberger <mg@protyposis.net>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurio.Project;
using System.Diagnostics;
using Aurio.Streams;
using Aurio.DataStructures.Graph;

namespace Aurio.Matching
{
    public class MatchProcessor
    {


        public static List<Match> FilterDuplicateMatches(List<Match> matches, Action<double> progressCallback)
        {
            List<Match> filteredMatches = new List<Match>();
            Dictionary<TimeSpan, List<Match>> filter = new Dictionary<TimeSpan, List<Match>>();
            long total = matches.Count;
            long index = 0;
            foreach (Match match in matches)
            {
                bool duplicateFound = false;
                // at first group the matches by the sum of their matching times (only matches with the same sum can be duplicates)
                TimeSpan sum = match.Track1Time + match.Track2Time;
                if (!filter.ContainsKey(sum))
                {
                    filter[sum] = new List<Match>();
                }
                else
                {
                    // if there are matches with the same time sum, check if they're indeed duplicates
                    foreach (Match sumMatch in filter[sum])
                    {
                        if (((sumMatch.Track1 == match.Track1 && sumMatch.Track2 == match.Track2 && sumMatch.Track1Time == match.Track1Time)
                            || (sumMatch.Track1 == match.Track2 && sumMatch.Track2 == match.Track1 && sumMatch.Track1Time == match.Track2Time))
                            && sumMatch.Similarity == match.Similarity)
                        {
                            // duplicate match found
                            duplicateFound = true;
                            break;
                        }
                    }
                }
                if (!duplicateFound)
                {
                    filter[sum].Add(match);
                    filteredMatches.Add(match);
                }

                // Report progress
                if (++index % 1000 == 0 || index == total)
                {
                    progressCallback(100d / total * index);
                }
            }
            return filteredMatches;
        }

        /// <summary>
        /// Detects duplicate matches and returns a list without those duplicates.
        /// A match is considered as a duplicate of another match, if both refer to the same two tracks 
        /// and positions within the tracks, and the similarity is the same (which automatically results from
        /// the identical track positions).
        /// </summary>
        /// <param name="matches">a list of matches to check for duplicates</param>
        /// <returns>a filtered list without duplicate matches</returns>
        public static List<Match> FilterDuplicateMatches(List<Match> matches)
        {
            return FilterDuplicateMatches(matches, (progress) => { });
        }

        /// <summary>
        /// Creates a list of all possible track pairs from a track list.
        /// </summary>
        public static List<MatchPair> GetTrackPairs(TrackList<AudioTrack> trackList)
        {
            List<MatchPair> pairs = new List<MatchPair>();
            for (int x = 0; x < trackList.Count; x++)
            {
                for (int y = x + 1; y < trackList.Count; y++)
                {
                    pairs.Add(new MatchPair { Track1 = trackList[x], Track2 = trackList[y] });
                }
            }
            return pairs;
        }

        /// <summary>
        /// Assigns a list of matches to their belonging track pairs.
        /// </summary>
        public static void AssignMatches(List<MatchPair> trackPairs, IEnumerable<Match> matches)
        {
            foreach (MatchPair trackPair in trackPairs)
            {
                List<Match> pairMatches = new List<Match>();
                foreach (Match match in matches)
                {
                    if (match.Track1 == trackPair.Track1 && match.Track2 == trackPair.Track2
                        || match.Track2 == trackPair.Track1 && match.Track1 == trackPair.Track2)
                    {
                        pairMatches.Add(match);
                    }
                }
                trackPair.Matches = pairMatches;
            }
        }

        /// <summary>
        /// Scans a collection of matches and returns a list of all affected audio tracks.
        /// </summary>
        private static List<AudioTrack> GetTracks(List<Match> matches)
        {
            List<AudioTrack> tracks = new List<AudioTrack>();

            foreach (Match match in matches)
            {
                if (!tracks.Contains(match.Track1))
                {
                    tracks.Add(match.Track1);
                }
                if (!tracks.Contains(match.Track2))
                {
                    tracks.Add(match.Track2);
                }
            }

            return tracks;
        }

        /// <summary>
        /// Validates that a collection of matches only belongs to two single distinct tracks.
        /// </summary>
        /// <see cref="GetTracks(List<List>)"/>
        public static void ValidatePair(List<Match> matches)
        {
            List<AudioTrack> tracks = GetTracks(matches);
            if (tracks.Count != 2)
            {
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
        public static void ValidatePairOrder(List<Match> matches)
        {
            ValidatePair(matches);
            List<AudioTrack> tracks = GetTracks(matches);
            AudioTrack t1 = tracks[0];
            AudioTrack t2 = tracks[1];

            foreach (Match match in matches)
            {
                if (match.Track1 != t1 || match.Track2 != t2)
                {
                    throw new Exception("pair order violated");
                }
            }
        }

        /// <summary>
        /// Filters a collection of matches for a pair of tracks according to the specified mode.
        /// </summary>
        /// <returns>a single match, chosen according to the specified match filter mode</returns>
        public static Match Filter(List<Match> matches, MatchFilterMode mode)
        {
            if (matches.Count == 0)
            {
                throw new ArgumentException("no matches to filter");
            }
            if (GetTracks(matches).Count != 2)
            {
                throw new ArgumentException("matches must contain a single pair of affected tracks");
            }

            if (mode == MatchFilterMode.Best)
            {
                return matches.OrderByDescending(m => m.Similarity).First();
            }
            else if (mode == MatchFilterMode.First)
            {
                return matches.OrderBy(m => m.Track1Time).First();
            }
            else if (mode == MatchFilterMode.Mid)
            {
                return matches.OrderBy(m => m.Track1Time).ElementAt(matches.Count() / 2);
            }
            else if (mode == MatchFilterMode.Last)
            {
                return matches.OrderBy(m => m.Track1Time).Last();
            }
            else
            {
                throw new NotImplementedException("mode not implemented: " + mode);
            }
        }

        /// <summary>
        /// Filters a collection of matches by applying a sliding window and determining the best match for
        /// each window according to the specified filter mode.
        /// </summary>
        /// <returns>a list of matches containing at least one match</returns>
        public static List<Match> WindowFilter(List<Match> matches, MatchFilterMode mode, TimeSpan windowSize, Action<double> progressCallback = null)
        {
            if (matches.Count == 0)
            {
                throw new ArgumentException("no matches to filter");
            }
            if (GetTracks(matches).Count != 2)
            {
                throw new ArgumentException("matches must contain a single pair of affected tracks");
            }

            // sort matches by time
            AudioTrack audioTrack = null;
            foreach (Match match in matches)
            {
                if (audioTrack == null)
                {
                    audioTrack = match.Track1;
                }
                else
                {
                    if (match.Track1 != audioTrack)
                    {
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
            long windowCount = matches.Last().Track1Time.Ticks / windowSize.Ticks;
            long currentWindowIndex = 0;

            while (filterWindowStart < matches.Last().Track1Time)
            {
                // get matches belonging to the current window
                foreach (Match match in matches)
                {
                    if (match.Track1Time >= filterWindowStart && match.Track1Time < filterWindowEnd)
                    {
                        filterWindowMatches.Add(match);
                    }
                }
                // process current window and switch to next window
                if (filterWindowMatches.Count > 0)
                {
                    filteredWindowMatches.Add(Filter(filterWindowMatches, mode));
                    filterWindowMatches.Clear();
                }
                filterWindowStart += filterWindow;
                filterWindowEnd += filterWindow;
                currentWindowIndex++;
                progressCallback?.Invoke(100d / windowCount * currentWindowIndex);
            }

            return filteredWindowMatches;
        }

        public static void Align(Match match, AudioTrack trackToAdjust)
        {
            if (trackToAdjust.Locked)
            {
                return; // don't move locked track!!
            }

            if (match.Track1 == trackToAdjust)
            {
                match.Track1.Offset = match.Track2.Offset + match.Track2Time - match.Track1Time;
            }
            else if (match.Track2 == trackToAdjust)
            {
                match.Track2.Offset = match.Track1.Offset + match.Track1Time - match.Track2Time;
            }
            else
            {
                throw new Exception("the track to adjust doesn't belong to the match");
            }
        }

        public static void Align(Match match)
        {
            if (!match.Track1.Locked && match.Track1.Offset + match.Track1Time < match.Track2.Offset + match.Track2Time)
            {
                // move track 1
                Align(match, match.Track1);
            }
            else if (!match.Track2.Locked)
            {
                // move track 2
                Align(match, match.Track2);
            }
        }

        /// <summary>
        /// Calculates timewarps from the supplied list of matches and returns a dictionary that associates
        /// each match with its resulting warp.
        /// The list of warps can then be used to apply warping to a track, and the dictionary serves the purpose
        /// that the matches can be updated with the warped times once warping has been applied.
        /// </summary>
        public static Dictionary<Match, TimeWarp> GetTimeWarps(List<Match> matches, AudioTrack trackToWarp)
        {
            if (matches.Count == 0)
            {
                throw new ArgumentException("no matches to filter");
            }
            if (GetTracks(matches).Count != 2)
            {
                throw new ArgumentException("matches must contain a single pair of affected tracks");
            }

            var timeWarps = new Dictionary<Match, TimeWarp>();

            if (matches.Count < 2)
            {
                // warping needs at least 2 matches that form a warped interval; nothing to do here
                return timeWarps;
            }

            AudioTrack track = trackToWarp;

            if (track == matches[0].Track1)
            {
                // since we always warp the second track and the requested track to warp is currently the
                // first one, the matches need to be swapped
                foreach (Match match in matches)
                {
                    match.SwapTracks();
                }
            }

            // calculate time warps from track's matches
            Match m1 = matches[0];
            Match m2 = null;
            timeWarps.Add(m1, new TimeWarp()
            { // the start of the warping section
                From = m1.Track2Time,
                To = m1.Track2Time
            });
            for (int i = 1; i < matches.Count; i++)
            {
                m2 = matches[i];
                TimeSpan targetTime = m1.Track2Time + (m2.Track1Time - m1.Track1Time);
                timeWarps.Add(m2, new TimeWarp()
                {
                    From = m2.Track2Time,
                    To = targetTime
                });
            }

            return timeWarps;
        }

        public static void ValidateMatches(List<MatchGroup> trackGroups)
        {
            foreach (MatchGroup trackGroup in trackGroups)
            {
                foreach (MatchPair trackPair in trackGroup.MatchPairs)
                {
                    var timeWarpCollection = new TimeWarpCollection();

                    // (By convention, we always warp the second track of a pair of tracks.)
                    var timeWarps = GetTimeWarps(trackPair.Matches, trackPair.Track2);

                    // Adding all warps to the collection triggers the validation.
                    // This will validate the warps and throw an exception if something is wrong.
                    timeWarpCollection.AddRange(timeWarps.Values);
                }
            }
        }

        public static AudioTrack TimeWarp(List<Match> matches, AudioTrack trackToWarp)
        {
            var timeWarps = GetTimeWarps(matches, trackToWarp);

            if (timeWarps.Count == 0)
            {
                // warping needs at least 1 warp; nothing to do here
                return trackToWarp;
            }

            // apply time warps to the track
            trackToWarp.TimeWarps.AddRange(timeWarps.Values);

            // Update the matches to reflect the changes induced by warping
            foreach (var entry in timeWarps)
            {
                entry.Key.Track2Time = entry.Value.To;
            }

            return trackToWarp;
        }

        public static void AlignTracks(List<MatchPair> trackPairs)
        {
            List<Match> allMatches = new List<Match>(); // create a new list for modification purposes
            List<AudioTrack> alignedTracks = new List<AudioTrack>();

            foreach (MatchPair trackPair in trackPairs)
            {
                allMatches.AddRange(trackPair.Matches);
                trackPair.Track1.TimeWarps.Clear();
                trackPair.Track2.TimeWarps.Clear();
            }

            // reorder list and put first locked track to top to start alignment at this track
            List<AudioTrack> allTracks = new List<AudioTrack>();
            foreach (MatchPair pair in trackPairs)
            {
                if (pair.Track2.Locked)
                    pair.SwapTracks();

                if (pair.Track1.Locked)
                {
                    int pairIndex = trackPairs.IndexOf(pair);
                    /* If the locked track is somewhere in the middle of the alignment sequence, the order of the preceding
                     * pairs needs to be reversed for the alignment sequence to be correct. What happens is, that a path of
                     * alignment pairs gets split into 2 subpaths, forming a tree with two branches. */
                    if (pairIndex > 0)
                    {
                        trackPairs.Reverse(0, pairIndex);
                    }
                    trackPairs.Remove(pair);
                    trackPairs.Insert(0, pair);
                    alignedTracks.Add(pair.Track1);
                    break;
                }
            }

            foreach (MatchPair trackPair in trackPairs)
            {
                AudioTrack trackToAlign = alignedTracks.Contains(trackPair.Track2) ?
                    trackPair.Track1 : trackPair.Track2;

                Align(trackPair.Matches[0], trackToAlign);
                Debug.WriteLine("aligned: " + trackToAlign);
                if (trackPair.Matches.Count > 1)
                {
                    TimeWarp(trackPair.Matches, trackToAlign);
                    Debug.WriteLine("warped: " + trackToAlign);

                    // adjust all other matches related to the currently warped track
                    List<Match> adjustedMatches = new List<Match>();
                    foreach (Match match in allMatches)
                    {
                        if (!trackPair.Matches.Contains(match))
                        {
                            if (match.Track1 == trackToAlign && !match.Track1.Locked)
                            {
                                match.Track1Time = trackToAlign.TimeWarps.TranslateSourceToWarpedPosition(match.Track1Time);
                                adjustedMatches.Add(match);
                            }
                            else if (match.Track2 == trackToAlign && !match.Track2.Locked)
                            {
                                match.Track2Time = trackToAlign.TimeWarps.TranslateSourceToWarpedPosition(match.Track2Time);
                                adjustedMatches.Add(match);
                            }
                        }
                    }
                    allMatches.RemoveAll(match => adjustedMatches.Contains(match));
                }

                alignedTracks.Add(trackToAlign);
            }
        }

        public static void MoveToStartTime(TrackList<AudioTrack> trackList, TimeSpan startTime)
        {
            TimeSpan start = trackList.Start;
            TimeSpan delta = startTime - start;
            foreach (AudioTrack audioTrack in trackList)
            {
                if (!audioTrack.Locked)
                    audioTrack.Offset += delta;
            }
        }

        /// <summary>
        /// Determines all groups of tracks that are connected through one or more matches.
        /// </summary>
        public static List<MatchGroup> DetermineMatchGroups(MatchFilterMode matchFilterMode, TrackList<AudioTrack> trackList,
                                              List<Match> matches, bool windowed, TimeSpan windowSize)
        {
            List<MatchPair> trackPairs = MatchProcessor.GetTrackPairs(trackList);
            MatchProcessor.AssignMatches(trackPairs, matches);
            trackPairs = trackPairs.Where(matchPair => matchPair.Matches.Count > 0).ToList(); // remove all track pairs without matches

            // filter matches
            foreach (MatchPair trackPair in trackPairs)
            {
                List<Match> filteredMatches;

                if (trackPair.Matches.Count > 0)
                {
                    if (matchFilterMode == MatchFilterMode.None)
                    {
                        filteredMatches = trackPair.Matches;
                    }
                    else
                    {
                        if (windowed)
                        {
                            filteredMatches = MatchProcessor.WindowFilter(trackPair.Matches, matchFilterMode, windowSize);
                        }
                        else
                        {
                            filteredMatches = new List<Match>();
                            filteredMatches.Add(MatchProcessor.Filter(trackPair.Matches, matchFilterMode));
                        }
                    }

                    trackPair.Matches = filteredMatches;
                }
            }

            // determine connected tracks
            UndirectedGraph<AudioTrack, double> trackGraph = new UndirectedGraph<AudioTrack, double>();
            foreach (MatchPair trackPair in trackPairs)
            {
                trackGraph.Add(new Edge<AudioTrack, double>(trackPair.Track1, trackPair.Track2, 1d - trackPair.CalculateAverageSimilarity())
                {
                    Tag = trackPair
                });
            }

            List<UndirectedGraph<AudioTrack, double>> trackGraphComponents = trackGraph.GetConnectedComponents();
            Debug.WriteLine("{0} connected components", trackGraphComponents.Count);

            List<MatchGroup> trackGroups = new List<MatchGroup>();
            foreach (UndirectedGraph<AudioTrack, double> component in trackGraphComponents)
            {
                List<MatchPair> connectedTrackPairs = new List<MatchPair>();

                Debug.WriteLine("determining connected track pairs...");
                foreach (Edge<AudioTrack, double> edge in component.GetMinimalSpanningTree().Edges)
                {
                    connectedTrackPairs.Add((MatchPair)edge.Tag);
                }
                Debug.WriteLine("finished - {0} pairs", connectedTrackPairs.Count);

                foreach (MatchPair filteredTrackPair in connectedTrackPairs)
                {
                    Debug.WriteLine("TrackPair {0} <-> {1}: {2} matches, similarity = {3}",
                        filteredTrackPair.Track1, filteredTrackPair.Track2,
                        filteredTrackPair.Matches.Count, filteredTrackPair.CalculateAverageSimilarity());
                }

                TrackList<AudioTrack> componentTrackList = new TrackList<AudioTrack>(component.Vertices);

                trackGroups.Add(new MatchGroup
                {
                    MatchPairs = connectedTrackPairs,
                    TrackList = componentTrackList
                });
            }

            return trackGroups;
        }

        /// <summary>
        /// Filters out matches that are coincident with another match. Two matches are coincident if they point
        /// to the same time in the audio stream of one of the two involved tracks.
        /// Coincident are a problem when a tracks gets adjusted/resampled to match another track. A section of one
        /// track cannot be resampled to an infinite small section of another track, so every match but the first at
        /// one instance in time gets thrown away (filtered out).
        /// </summary>
        /// <remarks>
        /// This is actually a workaround. The problem should be taken care of in the TimeWarpStream by just
        /// skipping the section of a track that should be resampled to an infinitely small section according
        /// to the coincident matches.
        /// </remarks>
        /// <param name="trackPairs"></param>
        public static void FilterCoincidentMatches(List<MatchPair> trackPairs)
        {
            foreach (MatchPair matchPair in trackPairs)
            {
                List<Match> filteredMatches = new List<Match>();
                Match previousMatch = null;
                foreach (Match match in matchPair.Matches)
                {
                    if (previousMatch != null &&
                        (previousMatch.Track1Time == match.Track1Time || previousMatch.Track2Time == match.Track2Time))
                    {
                        // skip this match
                    }
                    else
                    {
                        filteredMatches.Add(match);
                        previousMatch = match;
                    }
                }
                matchPair.Matches = filteredMatches;
            }
        }

        /// <summary>
        /// Converts a list of sorted matches between two tracks into a list of intervals, by grouping all consecutive matches
        /// whose offset differences stay below a specific threshold into one interval.
        /// If track B contains two excerpts from track A, the result will be two intervals that tell which excerpts of track A
        /// went into track B.
        ///
        /// Track A: AAAAAAAAAAXXXXXXXXXAAAAAAAYYYYYAAAAA
        /// Track B: XXXXXXXXXYYYYY -> two intervals
        /// </summary>
        /// <param name="matches">A list of matches between two tracks</param>
        /// <param name="thresholdMillisecs">The maximum drift between consecutive matches to count for the same interval</param>
        /// <returns>A list of mapped intervals between the two tracks</returns>
        public static List<Tuple<Interval, Interval>> ConvertToIntervals(List<Match> matches, int thresholdMillisecs = 1000)
        {
            ValidatePairOrder(matches);

            long thresholdTicks = new TimeSpan(0, 0, 0, 0, thresholdMillisecs).Ticks;
            long previousOffset = 0;
            long processedMatches = 0;
            Match intervalStartMatch = null;
            Match previousMatch = null;
            var intervals = new List<Tuple<Interval, Interval>>();

            foreach (Match match in matches)
            {
                long offset = match.Offset.Ticks;

                if (processedMatches == 0)
                {
                    previousOffset = offset;
                    intervalStartMatch = match;
                    previousMatch = match;
                    processedMatches++;
                    continue;
                }

                if (Math.Abs(previousOffset - offset) > thresholdTicks)
                {
                    // Offset is off, a new interval probably begins
                    var sourceInterval = new Interval(intervalStartMatch.Track1Time.Ticks, previousMatch.Track1Time.Ticks);
                    var destinationInterval = new Interval(intervalStartMatch.Track2Time.Ticks, previousMatch.Track2Time.Ticks);
                    intervals.Add(Tuple.Create(sourceInterval, destinationInterval));

                    intervalStartMatch = match;
                }

                previousOffset = offset;
                previousMatch = match;
                processedMatches++;
            }

            // Finish last interval
            var sourceIntervalEnd = new Interval(intervalStartMatch.Track1Time.Ticks, previousMatch.Track1Time.Ticks);
            var destinationIntervalEnd = new Interval(intervalStartMatch.Track2Time.Ticks, previousMatch.Track2Time.Ticks);
            intervals.Add(Tuple.Create(sourceIntervalEnd, destinationIntervalEnd));

            return intervals;
        }
    }
}
