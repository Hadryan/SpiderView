﻿using Spider.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;
namespace Spider
{
    public class DummyService : IMusicService
    {
        public static String CONNECTION_PATH = "Provider=Microsoft.Jet.OLEDB.4.0; Data Source=myspotify.mdb;Jet OLEDB:Database Password=;";
        public OleDbConnection MakeConnection()
        {
            return new OleDbConnection(CONNECTION_PATH);
        }
        public DummyService()
        {

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += timer_Tick;
        }
        private Track nowPlayingTrack;
        public Track NowPlayingTrack
        {
            get
            {
                return nowPlayingTrack;
            }
        }
        void timer_Tick(object sender, EventArgs e)
        {
            if (NowPlayingTrack == null)
                return;

            position++;
            if (position >= nowPlayingTrack.Duration)
            {
                if (PlaybackFinished != null)
                {
                    timer.Stop();
                    position = 0; // Reset the position
                    PlaybackFinished(this, new EventArgs());
                    //     nowPlayingTrack = null;
                }

            }
        }
        public event PlayStateChangedEventHandler PlaybackFinished;
        public string Namespace
        {
            get { return "dummy"; }
        }
        /// <summary>
        /// Simulate the track playback by a timer
        /// </summary>
        System.Windows.Forms.Timer timer;
        public string Name
        {
            get { return "Dumify"; }
        }
        int position = 0;
        private Track CurrentTrack;
        public void Play(Track track)
        {
            this.nowPlayingTrack = track;

            timer.Start();

        }

        public void Stop()
        {
            if (nowPlayingTrack == null)
                return;
            timer.Stop(); // Stop "playback"
            position = 0; // Reset position
            nowPlayingTrack = null;
        }

        public void Pause()
        {
            if (nowPlayingTrack == null)
                return;
            timer.Stop();
        }

        public void Seek(int pos)
        {
            if (CurrentTrack == null)
                return;
            position = pos;
            if (position >= CurrentTrack.Duration)
            {
                if (PlaybackFinished != null)
                {
                    timer.Stop();
                    position = 0; // Reset the position
                    PlaybackFinished(this, new EventArgs());
                    nowPlayingTrack = null;
                }

            }
        }

        public Artist LoadArtist(string identifier)
        {
            DataSet artists = MakeDataSet("SELECT * FROM artist WHERE identifier= '" + identifier + "'");
            DataRow artistRow = artists.Tables[0].Rows[0];
            

            return ArtistFromDataSet(artistRow);
        }


        public Playlist PlaylistFromRow(DataRow row)
        {
            Playlist playlist = new Playlist(this);
            playlist.Name = (String)row["title"];
            playlist.Description = (String)row["playlist.description"];
            playlist.Image = (String)row["playlist.image"];
            playlist.Status = Resource.State.Available;
            playlist.User = new User(this)
            {
                Name = (String)row["users.identifier"],
                Identifier = (String)row["users.identifier"]
            };
            playlist.Identifier =(String)row["playlist.identifier"];
            return playlist;
        }
        public Playlist LoadPlaylist(string username, string identifier)
        {
            DataSet dr = MakeDataSet("SELECT * FROM users, playlist WHERE playlist.user = users.id AND users.identifier = '" + username + "' AND playlist.identifier = '" + identifier + "'");
            Playlist pl = PlaylistFromRow(dr.Tables[0].Rows[0]);
            return pl;
        }
        public Dictionary<String, Track> Cache = new Dictionary<string, Track>();
        public SearchResult Find(string query, int maxResults, int page)
        {
            // Find songs
            DataSet tracksResult = MakeDataSet("SELECT * FROM track,artist, release WHERE artist.id = track.artist AND track.album = release.id AND (track.title LIKE '%" + query + "%' OR track.title LIKE '%%') AND (release.title LIKE '%" + query + "%' OR release.title LIKE '%%') AND (artist.title LIKE '%" + query + "%' OR artist.title = '%%')");
            SearchResult sr = new SearchResult(this);
            sr.Tracks = new TrackCollection(this, sr, new List<Track>());
            foreach (DataRow dr in tracksResult.Tables[0].Rows)
            {
                
                sr.Tracks.Add(TrackFromDataRow(dr));
            }

            // Find artists
            DataSet artistsResult = MakeDataSet("SELECT * FROM artist WHERE title LIKE '%" + query + "%'");
            ArtistCollection ac = new ArtistCollection(this, new List<Artist>());
            foreach (DataRow dr in artistsResult.Tables[0].Rows)
            {
                ac.Add(ArtistFromDataSet(dr));
            }
            sr.Artists = ac;

            // Find albums
            DataSet albumsResult = MakeDataSet("SELECT * FROM release, artist WHERE artist.id = release.artist AND release.title LIKE '%" + query + "%'");
            ReleaseCollection rc = new ReleaseCollection(this, new List<Release>());
            foreach (DataRow dr in albumsResult.Tables[0].Rows)
            {
                rc.Add(ReleaseFromDataRow(dr));
            }
            sr.Albums = rc;
            return sr;
        }

        public Artist ArtistFromDataSet(DataRow artistRow)
        {
            Artist artist = new Artist(this);
            artist.Name = (String)artistRow["title"];
            artist.Image = (String)artistRow["image"];
            artist.Identifier = (String)artistRow["identifier"];
            return artist;
        }
        public Release ReleaseFromDataRow(DataRow releaseRow)
        {
            Release r = new Release(this);
            r.Name = (String)releaseRow["release.title"];
            r.Image = (String)releaseRow["release.image"];
            r.Identifier = (String)releaseRow["release.identifier"];
           
                r.Status = (Spider.Media.Resource.State)releaseRow["release.status"];
            
            r.Artist = new Artist(this)
            {
                Name = (String)releaseRow["artist.title"],
                Identifier = (String)releaseRow["artist.identifier"]
            };
            return r;
        }
        public Track TrackFromDataRow(DataRow dr)
        {
            Track t = new Track(this)
            {
                Name = (String)dr["track.title"],
                Identifier = (String)dr["track.identifier"],
                Artists = new Artist[] {
                        new Artist(this) {
                            Name = (String)dr["artist.title"],
                            Identifier = (String)dr["artist.identifier"]
                        }
                    },
                Album = new Release(this)
                {
                    Identifier = (String)dr["release.identifier"],
                    Name = (String)dr["release.title"],
                    Status = (Spider.Media.Resource.State)dr["release.status"],

                }
            };
            return t;
        }

        public ReleaseCollection LoadReleasesForGivenArtist(Artist artist, ReleaseType type, int page)
        {
            List<Release> items = new List<Release>();
            DataSet dsReleases = MakeDataSet("SELECT *, release.status FROM release, artist  WHERE artist.id = release.artist AND artist.identifier = '" + artist.Identifier + "' AND type = " + ((int)type).ToString() + " AND release.status = 0 ORDER BY release_date DESC");
           ReleaseCollection rc = new ReleaseCollection(this, items);
            foreach (DataRow releaseRow in dsReleases.Tables[0].Rows)
            {
                Release r = ReleaseFromDataRow(releaseRow);
                items.Add(r);
                
            }
            return rc;
        }

        public TrackCollection LoadTracksForPlaylist(Playlist playlist)
        {
            Thread.Sleep(1000);
            TrackCollection tc = new TrackCollection(this, playlist, new List<Track>());
            for (var i = 0; i < 3; i++)
            {
                Track track = new Track(this)
                {
                    Identifier = "5124525ffs12",
                    Name = "Test",
                    Artists = new Artist[] { new Artist(this) { Name = "TestArtist", Identifier = "2FOU" } }
                };
                tc.Add(track);
            }
            return tc;
        }

        public Release LoadRelease(string identifier)
        {
            OleDbConnection conn = MakeConnection();
            String sql = "SELECT * FROM release, artist WHERE artist.id = release.artist AND release.identifier = '" + identifier + "'";
            OleDbDataAdapter oda = new OleDbDataAdapter(sql, conn);
            DataSet ds = new DataSet();
            oda.Fill(ds);
            Release r = ReleaseFromDataRow(ds.Tables[0].Rows[0]);
            conn.Close();
            return r;
        }


        public DataSet MakeDataSet(String sql)
        {
            OleDbConnection conn = MakeConnection();
            conn.Open();
            OleDbDataAdapter oda = new OleDbDataAdapter(sql, conn);
            DataSet ds = new DataSet();
            oda.Fill(ds);
            conn.Close();
            return ds;
        }

        public TrackCollection LoadTracksForGivenRelease(Release release)
        {
            String sql = "SELECT * FROM track, release, artist WHERE (release.ID = track.album) AND (artist.ID = track.artist) AND (artist.ID = track.artist)  AND " +
                "release.identifier = '" + release.Identifier + "'";
            DataSet result = MakeDataSet(sql);
            TrackCollection tc = new TrackCollection(this, release, new List<Track>());
            foreach (DataRow row in result.Tables[0].Rows)
            {
                Track t = TrackFromDataRow(row);
                tc.Add(t);
            }

            return tc;

        }


        public Track LoadTrack(string identifier)
        {
            if (Cache.ContainsKey(identifier))
            {
                return Cache[identifier];
            }
            DataSet trackSet = MakeDataSet("SELECT track.status AS status, artist.title AS artist_title, artist.[identifier] AS artist_identifier, release.[identifier] AS release_identifier, release.[title] AS release_title, track.title " +
                "FROM (artist INNER JOIN release ON artist.ID = release.artist) INNER JOIN track ON (release.ID = track.album) AND (artist.ID = track.artist) AND (artist.ID = track.artist)" + 
                "WHERE (((track.identifier)=\"" + identifier + "\"));");
            Track t = new Track(this);
            DataRow track = trackSet.Tables[0].Rows[0];
            t.Name = (String)track["title"];
            t.Identifier = identifier;
            t.Status = (Spider.Media.Resource.State)track["status"];
            t.Artists = new Artist[] { new Artist(this) { Name = (String)track["artist_title"], Identifier = (String)track["artist_identifier"] } };
            t.Album = new Release(this)
            {
                Name = (String)track["release_title"],
                Identifier = (String)track["release_identifier"]
            };


            try
            {
                Cache.Add(identifier, t);
            }
            catch (Exception e)
            {
                return Cache[identifier];
            }
            return t;
        }


        public TopList LoadTopListForResource(Resource res)
        {
            throw new NotImplementedException();
        }

        public User LoadUser(string identifier)
        {
            return new User(this)
            {
                Name = identifier,
                CanoncialName = identifier
            };
        }


        public SessionState SessionState
        {
            get { return SessionState.LoggedIn; }
        }

        public LogInResult LogIn(string userName, string passWord)
        {
            return LogInResult.Success;
        }

        public User GetCurrentUser()
        {
            return new User(this)
            {
                Name = "Test"
            };
        }


        public bool InsertTrack(Playlist playlist, Track track, int pos)
        {
            return true;   
        }

        public bool ReorderTracks(Playlist playlist, int startPos, int count, int newPos)
        {
            return true;
        }

        public bool DeleteTrack(Playlist playlist, Track track)
        {
            return true;
        }
        public PlaylistTrack MakeUserTrackFromString(String row)
        {
            String[] parts = row.Split(':');
            PlaylistTrack track = new PlaylistTrack(this, parts[2], parts[4]);
            return track;
        }
        public TrackCollection GetPlaylistTracks(Playlist playlist, int revision)
        {
            DataSet ds = MakeDataSet("SELECT data FROM [playlist], [users] WHERE [users].[id] = [playlist].[user] AND [playlist].[identifier] = '" + playlist.Identifier + "' AND [users].[identifier] = '" + playlist.User.Identifier + "'");
            String d = (String)ds.Tables[0].Rows[0]["data"];
            String[] tracks = d.Split('&');
            TrackCollection tc = new TrackCollection(this, playlist, new List<Track>());
            foreach (String strtrack in tracks)
            {
                PlaylistTrack pt = MakeUserTrackFromString(strtrack);
                tc.Add((Track)pt);
            }
            return tc;
        }
    }
}
