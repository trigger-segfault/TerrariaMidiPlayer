using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Sanford.Multimedia.Midi
{
	public struct TempoDuration {
		public int Tempo;
		// In Ticks
		public int Start;
		// In Ticks
		public int Length;
	}

    public class Sequencer : IComponent
    {
        private Sequence sequence = null;

        private List<IEnumerator<int>> enumerators = new List<IEnumerator<int>>();

        private MessageDispatcher dispatcher = new MessageDispatcher();

        private ChannelChaser chaser = new ChannelChaser();

        private ChannelStopper stopper = new ChannelStopper();

        private MidiInternalClock clock = new MidiInternalClock();

		private List<TempoDuration> tempoDurations = new List<TempoDuration>();

        private int tracksPlayingCount;

        private readonly object lockObject = new object();

        private bool playing = false;

        private bool disposed = false;

        private ISite site = null;

		private long rawDuration = 0;
		private int duration = 0;

		#region Events

		public event EventHandler PlayingCompleted;

        public event EventHandler<ChannelMessageEventArgs> ChannelMessagePlayed
        {
            add
            {
                dispatcher.ChannelMessageDispatched += value;
            }
            remove
            {
                dispatcher.ChannelMessageDispatched -= value;
            }
        }

        public event EventHandler<SysExMessageEventArgs> SysExMessagePlayed
        {
            add
            {
                dispatcher.SysExMessageDispatched += value;
            }
            remove
            {
                dispatcher.SysExMessageDispatched -= value;
            }
        }

        public event EventHandler<MetaMessageEventArgs> MetaMessagePlayed
        {
            add
            {
                dispatcher.MetaMessageDispatched += value;
            }
            remove
            {
                dispatcher.MetaMessageDispatched -= value;
            }
        }

        public event EventHandler<ChasedEventArgs> Chased
        {
            add
            {
                chaser.Chased += value;
            }
            remove
            {
                chaser.Chased -= value;
            }
        }

        public event EventHandler<StoppedEventArgs> Stopped
        {
            add
            {
                stopper.Stopped += value;
            }
            remove
            {
                stopper.Stopped -= value;
            }
        }

        #endregion

        public Sequencer()
        {
            dispatcher.MetaMessageDispatched += delegate(object sender, MetaMessageEventArgs e)
            {
                if(e.Message.MetaType == MetaType.EndOfTrack)
                {
                    tracksPlayingCount--;

                    if(tracksPlayingCount == 0)
                    {
                        Stop();

                        OnPlayingCompleted(EventArgs.Empty);
                    }
                }
                else
                {
                    clock.Process(e.Message);
                }
            };

            dispatcher.ChannelMessageDispatched += delegate(object sender, ChannelMessageEventArgs e)
            {
                stopper.Process(e.Message);
            };

            clock.Tick += delegate(object sender, EventArgs e)
            {
                lock(lockObject)
                {
                    if(!playing)
                    {
                        return;
                    }

                    foreach(IEnumerator<int> enumerator in enumerators)
                    {
                        enumerator.MoveNext();
                    }
                }
            };
        }

        ~Sequencer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                lock(lockObject)
                {
                    Stop();

                    clock.Dispose();

                    disposed = true;

                    GC.SuppressFinalize(this);
                }
            }
        }

        public void Start()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion           

            lock(lockObject)
            {
                Stop();

                Position = 0;

                Continue();
            }
        }

        public void Continue()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            #region Guard

            if(Sequence == null)
            {
                return;
            }

            #endregion

            lock(lockObject)
            {
                Stop();

                enumerators.Clear();

                foreach(Track t in Sequence)
                {
                    enumerators.Add(t.TickIterator(Position, chaser, dispatcher).GetEnumerator());
                }

                tracksPlayingCount = Sequence.Count;

                playing = true;
                clock.Ppqn = sequence.Division;
                clock.Continue();
            }
        }

        public void Stop()
        {
            #region Require

            if(disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            lock(lockObject)
            {
                #region Guard

                if(!playing)
                {
                    return;
                }

                #endregion

                playing = false;
                clock.Stop();
                stopper.AllSoundOff();
            }
        }

        protected virtual void OnPlayingCompleted(EventArgs e)
        {
            EventHandler handler = PlayingCompleted;

            if(handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnDisposed(EventArgs e)
        {
            EventHandler handler = Disposed;

            if(handler != null)
            {
                handler(this, e);
            }
        }

		public bool IsPlaying {
			get { return playing; }
		}

        public int Position
        {
            get
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                #endregion

                return clock.Ticks;
            }
            set
            {
                #region Require

                if(disposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                else if(value < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                #endregion

                bool wasPlaying;

                lock(lockObject)
                {
                    wasPlaying = playing;

                    Stop();

                    clock.SetTicks(value);
                }

                lock(lockObject)
                {
                    if(wasPlaying)
                    {
                        Continue();
                    }
                }
            }
        }

        public Sequence Sequence
        {
            get
            {
                return sequence;
            }
            set
            {
                #region Require

                if(value == null)
                {
                    throw new ArgumentNullException();
                }
                else if(value.SequenceType == SequenceType.Smpte)
                {
                    throw new NotSupportedException();
                }

				#endregion


				lock (lockObject) {
					Stop();
					sequence = value;
				}
				GetTempoDurations();
			}
        }

		public double Speed {
			get { return clock.Speed; }
			set { clock.Speed = value; }
		}

		public int ProgressToTicks(double progress) {
			if (tempoDurations.Count <= 1) {
				return (int)((double)sequence.GetLength() * progress);
			}
			else {
				double passed = 0;
				double passedNext = 0;
				foreach (TempoDuration tempoDuration in tempoDurations) {
					passedNext += ((double)tempoDuration.Tempo * (double)tempoDuration.Length / (double)rawDuration);
					if (progress <= passedNext) {
						double ratio = (progress - passed) / ((double)tempoDuration.Tempo * (double)tempoDuration.Length / (double)rawDuration);
						return tempoDuration.Start + (int)(Math.Round(tempoDuration.Length * ratio));
					}
					passed = passedNext;
				}
			}
			return -1;
		}

		public int TicksToMilliseconds(int ticks) {
			if (tempoDurations.Count == 0) {
				return (int)((double)rawDuration / (double)clock.Ppqn / 1000.0 * clock.Speed);
			}
			else {
				long totalDuration = 0;
				int ticksPassed = 0;
				foreach (TempoDuration tempoDuration in tempoDurations) {
					ticksPassed += tempoDuration.Length;
					if (ticks <= ticksPassed) {
						return (int)((double)(totalDuration + (long)tempoDuration.Tempo * ((long)ticks - (long)tempoDuration.Start)) / (double)clock.Ppqn / 1000.0 * clock.Speed);
					}
					totalDuration += (long)tempoDuration.Tempo * (long)tempoDuration.Length;
				}
				return -1;
			}
		}

		public int Duration {
			get { return (int)(duration * clock.Speed); }
		}
		public int CurrentTime {
			get { return TicksToMilliseconds(clock.Ticks); }
		}
		public double CurrentProgress {
			get { return (double)TicksToMilliseconds(clock.Ticks) / (double)Duration; }
		}

		#region IComponent Members

		public event EventHandler Disposed;

        public ISite Site
        {
            get
            {
                return site;
            }
            set
            {
                site = value;
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            #region Guard

            if(disposed)
            {
                return;
            }

            #endregion

            Dispose(true);
        }

		#endregion

		private void GetTempoDurations() {
			tempoDurations.Clear();
			clock.Ppqn = sequence.Division;
			clock.Tempo = 500000;
			rawDuration = 0;

			bool tempoTrackFound = false;
			TempoDuration tempoDuration = new TempoDuration();
			foreach (Track t in Sequence) {
				IEnumerator <MidiEvent> midiEnumerator = t.Iterator().GetEnumerator();
				while (midiEnumerator.MoveNext()) {
					MidiEvent e = midiEnumerator.Current;
					if (e.MidiMessage.MessageType == MessageType.Meta) {
						var meta = (MetaMessage)e.MidiMessage;
						if (meta.MetaType == MetaType.Tempo) {
							tempoTrackFound = true;
							TempoChangeBuilder builder = new TempoChangeBuilder(meta);
							int newTempo = builder.Tempo;
							if (tempoDuration.Tempo != 0) {
								tempoDuration.Length = e.AbsoluteTicks - tempoDuration.Start;
								rawDuration += (long)tempoDuration.Tempo * (long)tempoDuration.Length;
								tempoDurations.Add(tempoDuration);
								tempoDuration = new TempoDuration();
							}
							tempoDuration.Tempo = newTempo;
							tempoDuration.Start = e.AbsoluteTicks;
						}
					}
				}

				if (tempoTrackFound)
					break;
			}
			if (tempoDuration.Tempo == 0)
				tempoDuration.Tempo = clock.Tempo;
			tempoDuration.Length = sequence.GetLength() - tempoDuration.Start;
			rawDuration += (long)tempoDuration.Tempo * (long)tempoDuration.Length;
			tempoDurations.Add(tempoDuration);

			// Assign the duration for quick use.
			double oldSpeed = Speed;
			Speed = 1.0;
			duration = TicksToMilliseconds(sequence.GetLength());
			Speed = oldSpeed;
		}
    }
}
