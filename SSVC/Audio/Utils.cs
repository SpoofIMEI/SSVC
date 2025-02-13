using CSCore.CoreAudioAPI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSVC.Audio
{
    internal class Utils
    {
        public static Dictionary<int, MMDevice> GetMics()
        {
            using var deviceEnumerator = new MMDeviceEnumerator();
            using var deviceCollection = deviceEnumerator.EnumAudioEndpoints(DataFlow.Capture, DeviceState.Active);
            Dictionary<int, MMDevice> mics = new();
            for (var i = 0; i < deviceCollection.Count; i++)
                mics[i] = deviceCollection[i];
            return mics;
        }

        public static Dictionary<int, MMDevice> GetSpeakers()
        {
            using var deviceEnumerator = new MMDeviceEnumerator();
            using var deviceCollection = deviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
            Dictionary<int, MMDevice> speakers = new();
            for (var i = 0; i < deviceCollection.Count; i++)
                speakers[i] = deviceCollection[i];
            return speakers;
        }
    }
}
