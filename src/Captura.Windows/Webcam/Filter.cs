using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using DirectShowLib;

namespace Captura.Webcam
{
    /// <summary>
    /// Represents a DirectShow video capture device filter
    /// </summary>
    public class Filter : IComparable
    {
        /// <summary> Human-readable name of the filter </summary>
        public string Name { get; }

        /// <summary> Unique string referencing this filter (moniker string) </summary>
        public string MonikerString { get; }

        /// <summary> Create a new filter from its moniker </summary>
        public Filter(IMoniker Moniker)
        {
            Name = GetName(Moniker);
            MonikerString = GetMonikerString(Moniker);
        }

        /// <summary> Retrieve the moniker's display name (unique identifier) </summary>
        static string GetMonikerString(IMoniker Moniker)
        {
            Moniker.GetDisplayName(null, null, out var s);
            return s;
        }

        /// <summary> Retrieve the human-readable name of the filter </summary>
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
                    return "Unknown Device";
                
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
        /// Compares the current instance with another object of the same type
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
        /// Enumerate all video input devices using DirectShow
        /// </summary>
        public static IEnumerable<Filter> VideoInputDevices
        {
            get
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

                    // Create an enumerator to find filters in the video input category
                    var hr = enumDev.CreateClassEnumerator(category, out enumMon, 0);
                    
                    if (hr != 0 || enumMon == null)
                        yield break;

                    // Collect filters in a list first (can't use yield in try-catch)
                    var filters = new List<Filter>();
                    
                    // Loop through all devices
                    while (true)
                    {
                        // Get next device
                        hr = enumMon.Next(1, mon, IntPtr.Zero);

                        if (hr != 0 || mon[0] == null)
                            break;

                        try
                        {
                            // Create filter object
                            var filter = new Filter(mon[0]);
                            
                            // Only collect filters with valid names
                            if (!string.IsNullOrEmpty(filter.Name) && filter.Name != "Unknown Device")
                            {
                                filters.Add(filter);
                            }
                        }
                        catch
                        {
                            // Skip devices that cause errors
                        }
                        finally
                        {
                            // Release the moniker
                            if (mon[0] != null)
                            {
                                Marshal.ReleaseComObject(mon[0]);
                                mon[0] = null;
                            }
                        }
                    }
                    
                    // Return collected filters
                    foreach (var filter in filters)
                    {
                        yield return filter;
                    }
                }
                finally
                {
                    // Cleanup
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
}
