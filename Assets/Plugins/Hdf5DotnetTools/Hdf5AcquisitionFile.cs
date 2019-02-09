﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Hdf5DotNetTools {
	public interface IHdf5AcquisitionFile {
		Hdf5Patient Patient { get; set; }
		Hdf5Recording Recording { get; set; }
		Hdf5Channel[] Channels { get; set; }
		List<Hdf5Event> EventList { get; }
		Hdf5Events Events { get; }
	}

	[Hdf5SaveAttribute(Hdf5Save.Save)]
	public class Hdf5AcquisitionFile : IHdf5AcquisitionFile {
		public Hdf5AcquisitionFile() {
			Patient = new Hdf5Patient();
			Recording = new Hdf5Recording();
			EventList = new List<Hdf5Event>();
			Events = new Hdf5Events();

			Recording.PropertyChanged += (sender, eventArgs) => {
				if (eventArgs.PropertyName == nameof(Hdf5Recording.NrOfChannels))
					Channels = new Hdf5Channel[Recording.NrOfChannels];
			};

		}

		public Hdf5Patient Patient { get; set; }
		public Hdf5Recording Recording { get; set; }

		//[Hdf5Save(Hdf5Save.DoNotSave)]
		//public Hdf5Channel[] Channels { get; set; }
		public Hdf5Channel[] Channels { get; set; }

		/*public Hdf5Event[] Events
        {
            get { return EventList.ToArray(); }
            private set
            {
                // When the array is read from the hdf5 file the EventList is created
                EventList = new List<Hdf5Event>(value);
            }
        }*/
		public Hdf5Events Events { get; set; }

		[Hdf5Save(Hdf5Save.DoNotSave)]
		public List<Hdf5Event> EventList { get; private set; }

		[Hdf5Save(Hdf5Save.DoNotSave)]
		public short[,] Data { get; set; }

		public void EventListToEvents() {
			Events = new Hdf5Events(EventList.Count);
			for (int i = 0; i < EventList.Count; i++) {
				Events.Times[i] = EventList[i].Time;
				Events.Durations[i] = EventList[i].Duration;
				Events.Events[i] = EventList[i].Event;
			}

		}

	}

	[Hdf5GroupName("Channels")]
	public class Hdf5Channels {
		public Hdf5Channels(int length) {
			Labels = new string[length];
			Dimensions = new string[length];
			Amplifications = new double[length];
			Offsets = new double[length];
			SamplingRates = new double[length];
			NrOfSamples = new int[length];
		}
		public string[] Labels { get; set; }
		public string[] Dimensions { get; set; }
		public double[] Amplifications { get; set; }
		public double[] Offsets { get; set; }
		public double[] SamplingRates { get; set; }
		public int[] NrOfSamples { get; set; }

	}

	[Hdf5GroupName("Channel")]
	[StructLayout(LayoutKind.Sequential)]
	public struct Hdf5Channel {
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
		public string Label;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)]
		public string Dimension;
		public double Amplification;
		public double Offset;
		public double SamplingRate;
		public ulong NrOfSamples;

	}

	[Hdf5GroupName("Recording")]
	public class Hdf5Recording {
		int _nrOfChannels;
		[Hdf5Save(Hdf5Save.DoNotSave)]
		public event PropertyChangedEventHandler PropertyChanged;

		public string Id { get; set; } = "";
		public bool ActiveFilter { get; set; } = false;
		public DateTime StartTime { get; set; } = DateTime.Now;
		public DateTime EndTime { get; set; } = DateTime.Now;
		public ulong NrOfSamples { get; set; } = 0;
		public double SampleRate { get; set; } = double.NaN;
		public string Physician { get; set; } = "";
		public string Laborant { get; set; } = "";

		public int NrOfChannels {
			get { return _nrOfChannels; }
			set {
				if (_nrOfChannels != value) {
					_nrOfChannels = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NrOfChannels)));
				}
			}
		}
	}

	[Hdf5GroupName("Patient")]
	public class Hdf5Patient {
		public string Name = "";
		public string Id = "";
		public int RecId = -1;
		public string Gender = "";
		public DateTime BirthDate = DateTime.Now;
		public double Height = double.NaN;
		public double Weight = double.NaN;
		public DateTime EditData = DateTime.Now;
	}

	[Hdf5GroupName("Events")]
	public struct Hdf5Events {
		public Hdf5Events(int length) {
			Events = new string[length];
			Times = new DateTime[length];
			Durations = new TimeSpan[length];
		}
		public string[] Events { get; set; }
		public DateTime[] Times { get; set; }
		public TimeSpan[] Durations { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	[Hdf5GroupName("Event")]
	[StructLayout(LayoutKind.Sequential)]
	public struct Hdf5Event {
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
		public string Event;

		/// <summary>
		/// Time property. Datetimes can't be saved so the TimeTicks field gets saved
		/// </summary>
		[Hdf5Save(Hdf5Save.DoNotSave)]
		public DateTime Time {
			get { return new DateTime(TimeTicks); }
			set {
				TimeTicks = value.Ticks;
			}
		}

		public long TimeTicks;

		/// <summary>
		/// Duration property. Timespans can't be saved so the DurationTicks field gets saved
		/// </summary>
		[Hdf5Save(Hdf5Save.DoNotSave)]
		public TimeSpan Duration {
			get { return new TimeSpan(DurationTicks); }
			set {
				DurationTicks = value.Ticks;
			}
		}

		public long DurationTicks;
	}
}
