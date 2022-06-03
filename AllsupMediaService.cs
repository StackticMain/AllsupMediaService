using System;
using System.Collections.Generic;

namespace Allsup.AllsupMediaService
{
    public class Media
    {
        private int Capacity { get; set; } = 36000; //seconds
        public int RemainingCapacity { get; set; }
        public Media() => this.RemainingCapacity = Capacity;
        public void UpdateMediaCapacity(int seconds) => RemainingCapacity -= seconds; 
        public bool HasCapacity() => RemainingCapacity >= 300; 
    }

    public static class SongsApi
    {
        public static Song GetNextSong() => new Song();
        public static void WriteSongToMedia(Song song) => Console.WriteLine("song written to media");
        public static bool FinalizeMedia() => true;
    }


    public interface IMediaProvider<T>{ IEnumerable<T> GetMedia();  }
    public interface IMediaValidator<T,V> { V Validate(T storagemedia); }
    public interface IMediaWriter<T> { void Write(T data); }
    public interface IMediaFinalizer<T,V> { bool Finalize(T storagemedia); }

    public class SongProvider : IMediaProvider<Song>
    {
        public IEnumerable<Song> GetMedia()
        {
            var songs = new List<Song>();
            while(SongsApi.GetNextSong() != null)
            {
                songs.Add(SongsApi.GetNextSong());
            }
            return songs;
        }
    }
    public class SongWriter : IMediaWriter<Song>
    {
        public void Write(Song song)
        {
            if(song != null)
            {
                SongsApi.WriteSongToMedia(song);
            }
        }
    }
    public class MediaValidator : IMediaValidator<Media, bool>
    {
        public bool Validate(Media storageMedia) => storageMedia.HasCapacity();
    }
    public class MediaFinalizer : IMediaFinalizer<Media, bool>
    {
        public bool Finalize(Media media) => SongsApi.FinalizeMedia();
    }
    
    public class Processor
    {
        private readonly IMediaProvider<Song> _mediaProvider;
        private readonly IMediaValidator<Media,bool> _mediaValidator;
        private readonly IMediaWriter<Song> _mediaWriter;
        private readonly IMediaFinalizer<Media,bool> _mediaFinalizer;

        public Processor(IMediaProvider<Song> mediaProvider, 
            IMediaValidator<Media, bool> mediaValidator, 
            IMediaWriter<Song> mediaWriter, 
            IMediaFinalizer<Media,bool> mediaFinalizer)
        {
            _mediaProvider = mediaProvider;
            _mediaValidator = mediaValidator;
            _mediaWriter = mediaWriter;
            _mediaFinalizer = mediaFinalizer;
        }

        public void Process()
        {
            var storageMedia = new Media();
            
            foreach(var item in _mediaProvider.GetMedia()) //100 songs
            {
                bool isStorageMediaValid = _mediaValidator.Validate(storageMedia); //has more than 5 mins of capacity?
                if(isStorageMediaValid)
                {
                    try
                    {
                        _mediaWriter.Write(item);
                        storageMedia.UpdateMediaCapacity(item.Duration);
                    }
                    catch(Exception ex)
                    {
                        throw new CapacityOverflowException("Could not write to media - the capacity has been exceeded", ex);
                    }
                    finally
                    {
                        storageMedia.UpdateMediaCapacity(-item.Duration); //since the song was not written
                        if (!_mediaFinalizer.Finalize(storageMedia))
                            storageMedia = new Media();
                        _mediaWriter.Write(item); // write the current item to the new storage media
                    }
                }
                else
                {
                    _mediaFinalizer.Finalize(storageMedia);
                    storageMedia = new Media();
                }
            }
        }
    }
    public class Program
    {
        public static void Main(String[] args)
        {
            var processor = new Processor(new SongProvider(), new MediaValidator(), new SongWriter(), new MediaFinalizer());
            processor.Process();

        }
    }
    public class CapacityOverflowException : Exception
    {
        public CapacityOverflowException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

}
