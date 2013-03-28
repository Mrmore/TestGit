using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
#if NETFX_CORE
using System.Net.Http;
#endif

namespace Microsoft.TimedText
{
    public class CaptionMarkerFactory
    {
        public CaptionMarkerFactory()
        {
            MarkerParser = new TimedTextMarkerParser();
            _previousMarkers = new Dictionary<string, MediaMarker>();
        }

        public CaptionMarkerFactory(IMarkerParser markerParser)
        {
            MarkerParser = markerParser;
            _previousMarkers = new Dictionary<string, MediaMarker>();
        }

        private IDictionary<string, MediaMarker> _previousMarkers;

        IMarkerParser _markerParser;
        public IMarkerParser MarkerParser
        {
            get { return _markerParser; }
            set { _markerParser = value; }
        }

        public event Action<IEnumerable<MediaMarker>> NewMarkers;
        public event Action<IEnumerable<MediaMarker>> MarkersRemoved;

        public IList<MediaMarker> ParseTtml(string ttml)
        {
            XDocument markerXml = XDocument.Parse(ttml);

            return MarkerParser.ParseMarkerCollection(markerXml, TimeSpan.Zero, TimeSpan.MaxValue)
                                                            .Cast<MediaMarker>()
                                                            .ToList();
        }

        public void UpdateMarkers(IList<MediaMarker> markers, bool forceRefresh)
        {
            var markersHash = markers.ToDictionary(i => i.Id, i => i);

            List<MediaMarker> newMarkers;
            List<MediaMarker> removedMarkers;

            if (forceRefresh)
            {
                newMarkers = markers.ToList();
                removedMarkers = _previousMarkers.Values.ToList();
            }
            else
            {
                newMarkers = markers.Where(i => !_previousMarkers.ContainsKey(i.Id)).ToList();
                removedMarkers = _previousMarkers.Values.Where(i => !markersHash.ContainsKey(i.Id)).ToList();
            }

            if (removedMarkers.Count > 0 && MarkersRemoved != null)
            {
                MarkersRemoved(removedMarkers);
            }

            if (newMarkers.Count > 0 && NewMarkers != null)
            {
                NewMarkers(newMarkers);
            }

            _previousMarkers = markersHash;
        }
    }
}
