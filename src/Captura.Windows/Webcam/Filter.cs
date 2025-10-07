using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using DirectShowLib;
using MediaFoundation;
using MediaFoundation.Misc;

namespace Captura.Webcam
{
    /// <summary>
    /// Represents a video capture device filter.
    /// Uses MediaFoundation for device enumeration with DirectShow fallback.
    /// </summary>
    class Filter : IComparable
    {
        /// <summary> Human-readable name of the filter </summary>
        public string Name { get; }

        /// <summary> Unique string referencing this filter </summary>
        public string MonikerString { get; }

        /// <summary> Create a new filter from device information </summary>
        public Filter(string name, string monikerString)
        {
            Name = name ?? "Unknown Device";
            MonikerString = monikerString ?? throw new ArgumentNullException(nameof(monikerString));
        }

        /// <summary> Create a new filter from DirectShow moniker (legacy) </summary>
        public Filter(IMoniker Moniker)
        {
            Name = GetName(Moniker);
            MonikerString = GetMonikerString(Moniker);
        }

        /// <summary> Retrieve the a moniker's display name </summary>
        static string GetMonikerString(IMoniker Moniker)
        {
            Moniker.GetDisplayName(null, null, out var s);
            return s;
        }

        /// <summary> Retrieve the human-readable name of the filter from DirectShow </summary>
        static string GetName(IMoniker Moniker)
        {
            object bagObj = null;

            try
            {
                var bagId = typeof(IPropertyBag).GUID;
                Moniker.BindToStorage(null, null, ref bagId, out bagObj);
                var bag = (IPropertyBag)bagObj;
                var hr = bag.Read("FriendlyName", out var val, null);

                if (hr != 0)
                    Marshal.ThrowExceptionForHR(hr);

                var ret = val as string;

                if (string.IsNullOrEmpty(ret))
                    throw new NotImplementedException("Device FriendlyName");
                
                return ret;
            }
            catch (Exception)
            {
                return "Unknown Device";
            }
            finally
            {
                if (bagObj != null)
                    Marshal.ReleaseComObject(bagObj);
            }
        }

        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        public int CompareTo(object Obj)
        {
            if (Obj == null)
                return 1;

            var f = (Filter)Obj;
            return string.Compare(Name, f.Name, StringComparison.Ordinal);
        }

        public override string ToString() => Name;

        /// <summary>
        /// Enumerate video input devices using MediaFoundation (preferred) with DirectShow fallback
        /// </summary>
        public static IEnumerable<Filter> VideoInputDevices
        {
            get
            {
                var devices = new List<Filter>();

                // Try MediaFoundation first (modern API)
                try
                {
                    var mfDevices = EnumerateMediaFoundationDevices();
                    devices.AddRange(mfDevices);
                }
                catch
                {
                    // MediaFoundation failed, will try DirectShow
                }

                // If no MediaFoundation devices found, fallback to DirectShow
                if (devices.Count == 0)
                {
                    try
                    {
                        var dsDevices = EnumerateDirectShowDevices();
                        devices.AddRange(dsDevices);
                    }
                    catch
                    {
                        // Both methods failed
                    }
                }

                return devices;
            }
        }

        static IEnumerable<Filter> EnumerateMediaFoundationDevices()
        {
            var devices = new List<Filter>();

            // Initialize MediaFoundation
            var hr = MFExterns.MFStartup(MF_VERSION.MF_SDK_VERSION, MFStartup.Full);
            if (hr < 0)
                return devices;

            try
            {
                // Create attributes for device enumeration
                IMFAttributes attributes;
                hr = MFExterns.MFCreateAttributes(out attributes, 1);
                if (hr < 0)
                    return devices;

                try
                {
                    // Set attribute to enumerate video capture devices
                    hr = attributes.SetGUID(
                        MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
                        CLSID.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID);

                    if (hr < 0)
                        return devices;

                    // Enumerate devices
                    IMFActivate[] activateArray;
                    int count;
                    hr = MFExterns.MFEnumDeviceSources(attributes, out activateArray, out count);

                    if (hr < 0 || count == 0)
                        return devices;

                    // Iterate through devices
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            var activate = activateArray[i];

                            // Get friendly name
                            string friendlyName;
                            int nameLength;
                            hr = activate.GetString(
                                MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME,
                                out friendlyName,
                                out nameLength);

                            if (hr < 0)
                                friendlyName = $"Camera {i + 1}";

                            // Get symbolic link (unique identifier)
                            string symbolicLink;
                            int linkLength;
                            hr = activate.GetString(
                                MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK,
                                out symbolicLink,
                                out linkLength);

                            if (hr < 0)
                                symbolicLink = Guid.NewGuid().ToString();

                            devices.Add(new Filter(friendlyName, symbolicLink));
                        }
                        catch
                        {
                            // Skip this device on error
                        }
                        finally
                        {
                            if (activateArray[i] != null)
                                Marshal.ReleaseComObject(activateArray[i]);
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(attributes);
                }
            }
            finally
            {
                MFExterns.MFShutdown();
            }

            return devices;
        }

        static IEnumerable<Filter> EnumerateDirectShowDevices()
        {
            object comObj = null;
            IEnumMoniker enumMon = null;
            var mon = new IMoniker[1];

            try
            {
                // Get the system device enumerator
                comObj = new CreateDevEnum();
                var enumDev = (ICreateDevEnum)comObj;

                var category = FilterCategory.VideoInputDevice;

                // Create an enumerator to find filters in category
                var hr = enumDev.CreateClassEnumerator(category, out enumMon, 0);
                if (hr != 0 || enumMon == null)
                    yield break;

                // Loop through the enumerator
                while (true)
                {
                    // Next filter
                    hr = enumMon.Next(1, mon, IntPtr.Zero);

                    if (hr != 0 || mon[0] == null)
                        break;

                    // Add the filter
                    yield return new Filter(mon[0]);

                    // Release resources
                    Marshal.ReleaseComObject(mon[0]);
                    mon[0] = null;
                }
            }
            finally
            {
                if (mon[0] != null)
                    Marshal.ReleaseComObject(mon[0]);

                if (enumMon != null)
                    Marshal.ReleaseComObject(enumMon);

                if (comObj != null)
                    Marshal.ReleaseComObject(comObj);
            }
        }
    }
}
