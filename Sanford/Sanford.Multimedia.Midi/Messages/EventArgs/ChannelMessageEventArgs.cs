using System;
using System.Collections.Generic;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    public class ChannelMessageEventArgs : EventArgs
    {
		private Track track;
        private ChannelMessage message;

        public ChannelMessageEventArgs(Track track, ChannelMessage message)
        {
			this.track = track;
			this.message = message;
        }

        public ChannelMessage Message
        {
            get
            {
                return message;
            }
        }

		public Track Track {
			get { return track; }
		}
    }
}
