using System;

namespace GifViewer
{
    [Serializable]
    internal class GifParsingException : Exception
    {
        public GifParsingException()
        {
        }

        public GifParsingException(string message) : base(message)
        {
        }

        public GifParsingException(string message, Exception innerException) : base(message, innerException)
        {
        }


    }
}