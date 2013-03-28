﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Net.Http;
using System.Threading;
#if SILVERLIGHT
#else
using Windows.Storage.Streams;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace Microsoft.VideoAdvertising
{
    public static class FreeWheelFactory
    {
#if SILVERLIGHT
        public static async Task<FWAdResponse> LoadSource(Uri source, CancellationToken cancellationToken)
#else
        public static IAsyncOperation<FWAdResponse> LoadSource(Uri source)
        {
            return AsyncInfo.Run(c => InternalLoadSource(source, c));
        }

        static async Task<FWAdResponse> InternalLoadSource(Uri source, CancellationToken cancellationToken)
#endif
        {
            using (var stream = await Extensions.LoadStreamAsync(source))
            {
#if SILVERLIGHT && !WINDOWS_PHONE
                return await TaskEx.Run(() => CreateFromSmartXml(stream), cancellationToken);
#else
                return await Task.Run(() => CreateFromSmartXml(stream), cancellationToken);
#endif
            }
        }

#if SILVERLIGHT
        public static FWAdResponse CreateFromSmartXml(Stream stream)
#else
        public static FWAdResponse CreateFromSmartXml(IInputStream stream)
        {
            return CreateFromSmartXml(stream.AsStreamForRead());
        }

        internal static FWAdResponse CreateFromSmartXml(Stream stream)
#endif
        {
            XDocument xDoc = XDocument.Load(stream);

            return LoadAdResponse(xDoc.Root);
        }

        internal static FWAdResponse LoadAdResponse(XElement element)
        {
            var result = new FWAdResponse();
            result.Version = (string)element.Attribute("version");
            result.CustomId = (string)element.Attribute("customId");
            var networkId = (string)element.Attribute("networkId");
            if (networkId != null)
            {
                result.NetworkId = int.Parse(networkId);
            }
            result.Diagnostic = (string)element.Element("diagnostic");
            result.CustomState = (string)element.Element("customState");

            var rendererManifestXml = element.Element("rendererManifest");
            if (rendererManifestXml != null)
            {
                result.RendererManifest = rendererManifestXml.Value;
                result.RendererManifest = (string)rendererManifestXml.Attribute("version");
            }

            var visitorXml = element.Element("visitor");
            if (visitorXml != null)
            {
                result.Visitor = LoadVisitor(visitorXml);
            }

            var siteSectionXml = element.Element("siteSection");
            if (siteSectionXml != null)
            {
                result.SiteSection = LoadSiteSection(siteSectionXml);
            }

            var adsXml = element.Element("ads");
            if (adsXml != null)
            {
                foreach (var adXml in adsXml.Elements("ad"))
                {
                    result.Ads.Add(LoadAd(adXml));
                }
            }

            var parametersXml = element.Element("parameters");
            if (parametersXml != null)
            {
                foreach (var parameterXml in parametersXml.Elements("parameter"))
                {
                    result.Parameters.Add(LoadParameter(parameterXml));
                }
            }

            var errorsXml = element.Element("errors");
            if (errorsXml != null)
            {
                foreach (var errorXml in errorsXml.Elements("error"))
                {
                    result.Errors.Add(LoadError(errorXml));
                }
            }

            var eventsXml = element.Element("eventCallbacks");
            if (eventsXml != null)
            {
                foreach (var eventXml in eventsXml.Elements("eventCallback"))
                {
                    result.EventCallbacks.Add(LoadEventCallback(eventXml));
                }
            }

            return result;
        }

        internal static FWEventCallback LoadEventCallback(XElement element)
        {
            var result = new FWEventCallback();
            result.Url = (string)element.Attribute("url");
            result.Name = (string)element.Attribute("name");
            result.Type = (FWCallbackType)Enum.Parse(typeof(FWCallbackType), (string)element.Attribute("type"), true);
            result.ShowBrowser = element.GetBoolAttribute("showBrowser", true);
            result.Use = (string)element.Attribute("use");

            var trackingUrlsXml = element.Element("trackingURLs");
            if (trackingUrlsXml != null)
            {
                foreach (var trackingUrlXml in trackingUrlsXml.Elements("url"))
                {
                    result.TrackingUrls.Add(LoadUrl(trackingUrlXml));
                }
            }

            return result;
        }

        internal static FWUrl LoadUrl(XElement element)
        {
            var result = new FWUrl();
            result.Name = (string)element.Attribute("name");
            result.Value = (string)element.Attribute("value");
            return result;
        }

        internal static FWError LoadError(XElement element)
        {
            var result = new FWError();
            result.Id = (string)element.Attribute("id");
            result.Severity = (string)element.Attribute("severity");
            result.Name = (string)element.Attribute("name");
            result.Context = (string)element.Element("context");
            return result;
        }

        internal static FWParameter LoadParameter(XElement element)
        {
            var result = new FWParameter();
            result.Name = (string)element.Attribute("name");
            result.Category = (string)element.Attribute("category");
            return result;
        }

        internal static FWVisitor LoadVisitor(XElement element)
        {
            var result = new FWVisitor();
            result.CustomId = (string)element.Attribute("customId");

            var headersXml = element.Element("httpHeaders");
            if (headersXml != null)
            {
                foreach (var headerXml in headersXml.Elements("httpHeader"))
                {
                    result.HttpHeaders.Add(LoadHttpHeader(headerXml));
                }
            }

            var stateXml = element.Element("state");
            if (stateXml != null)
            {
                var entryXml = stateXml.Element("entry");
                if (entryXml != null)
                {
                    result.State = LoadState(entryXml);
                }
            }

            return result;
        }

        internal static FWHttpHeader LoadHttpHeader(XElement element)
        {
            var result = new FWHttpHeader();

            result.Name = (string)element.Attribute("name");
            result.Value = (string)element.Attribute("value");

            return result;
        }

        internal static FWState LoadState(XElement element)
        {
            var result = new FWState();

            result.Name = (string)element.Attribute("name");
            result.Value = (string)element.Attribute("value");

            return result;
        }

        internal static FWAd LoadAd(XElement element)
        {
            var result = new FWAd();
            result.Id = (string)element.Attribute("adId");
            result.AdUnit = (string)element.Attribute("adUnit");
            result.BundleId = element.GetIntAttribute("bundleId");
            result.NoLoad = element.GetBoolAttribute("noLoad", false);
            result.NoPreload = element.GetBoolAttribute("noPreload", false);

            var creativesXml = element.Element("creatives");
            if (creativesXml != null)
            {
                foreach (var creativeXml in creativesXml.Elements("creative"))
                {
                    result.Creatives.Add(LoadCreative(creativeXml));
                }
            }

            return result;
        }

        internal static FWCreative LoadCreative(XElement element)
        {
            var result = new FWCreative();

            result.RedirectUrl = (string)element.Attribute("redirectUrl");
            result.AdUnit = (string)element.Attribute("adUnit");
            result.BaseUnit = (FWBaseUnit)Enum.Parse(typeof(FWBaseUnit), ((string)element.Attribute("baseUnit")).Replace("-", "_"), true);
            var durationString = (string)element.Attribute("duration");
            if (!string.IsNullOrEmpty(durationString))
            {
                result.Duration = TimeSpan.FromSeconds(float.Parse(durationString));
            }
            result.Id = (string)element.Attribute("creativeId");

            var creativeRenditionsXml = element.Element("creativeRenditions");
            if (creativeRenditionsXml != null)
            {
                foreach (var creativeRenditionXml in creativeRenditionsXml.Elements("creativeRendition"))
                {
                    result.CreativeRenditions.Add(LoadCreativeRendition(creativeRenditionXml));
                }
            }

            var parametersXml = element.Element("parameters");
            if (parametersXml != null)
            {
                foreach (var parameterXml in parametersXml.Elements("parameter"))
                {
                    result.Parameters.Add(LoadParameter(parameterXml));
                }
            }

            return result;
        }

        internal static FWCreativeRendition LoadCreativeRendition(XElement element)
        {
            var result = new FWCreativeRendition();

            result.Id = (string)element.Attribute("creativeRenditionId");
            result.ContentType = (string)element.Attribute("contentType");
            result.WrapperType = (string)element.Attribute("wrapperType");
            result.WrapperUrl = (string)element.Attribute("wrapperUrl");
            result.AdReplicaId = (string)element.Attribute("adReplicaId");
            result.Width = element.GetIntAttribute("width");
            result.Height = element.GetIntAttribute("height");
            result.Preference = (FWPreference)element.GetIntAttribute("preference");
            result.CreativeApi = (FWCreativeApi)Enum.Parse(typeof(FWCreativeApi), (string)element.Attribute("creativeApi"), true);

            var otherAssetsXml = element.Element("otherAsset");
            if (otherAssetsXml != null)
            {
                foreach (var otherAssetXml in otherAssetsXml.Elements("asset"))
                {
                    result.OtherAssets.Add(LoadAsset(otherAssetXml));
                }
            }

            var assetXml = element.Element("asset");
            if (assetXml != null)
            {
                result.Asset = LoadAsset(assetXml);
            }

            var parametersXml = element.Element("parameters");
            if (parametersXml != null)
            {
                foreach (var parameterXml in parametersXml.Elements("parameter"))
                {
                    result.Parameters.Add(LoadParameter(parameterXml));
                }
            }

            return result;
        }

        internal static FWAsset LoadAsset(XElement element)
        {
            var result = new FWAsset();

            result.Bytes = element.GetIntAttribute("bytes");
            result.ContentType = (string)element.Attribute("contentType");
            result.Id = (string)element.Attribute("id");
            result.Name = (string)element.Attribute("name");
            result.Url = (string)element.Attribute("url");
            result.MimeType = (string)element.Attribute("mimeType");
            result.Content = (string)element.Attribute("content");

            return result;
        }

        internal static FWSiteSection LoadSiteSection(XElement element)
        {
            var result = new FWSiteSection();

            result.Id = (string)element.Attribute("id");
            result.CustomId = (string)element.Attribute("customId");
            result.PageViewRandom = (string)element.Attribute("pageviewRandom");
            result.VideoPlayer = LoadVideoPlayer(element.Element("videoPlayer"));

            var adSlotsXml = element.Element("adSlots");
            if (adSlotsXml != null)
            {
                foreach (var adSlotXml in adSlotsXml.Elements("adSlot"))
                {
                    result.AdSlots.Add(LoadAdSlot(adSlotXml));
                }
            }

            return result;
        }

        internal static FWVideoPlayer LoadVideoPlayer(XElement element)
        {
            var result = new FWVideoPlayer();

            var videoAssetXml = element.Element("videoAsset");
            if (videoAssetXml != null)
            {
                result.VideoAsset = LoadVideoAsset(videoAssetXml);
            }

            var adSlotsXml = element.Element("adSlots");
            if (adSlotsXml != null)
            {
                foreach (var adSlotXml in adSlotsXml.Elements("adSlot"))
                {
                    result.AdSlots.Add(LoadAdSlot(adSlotXml));
                }
            }

            return result;
        }

        internal static FWVideoAsset LoadVideoAsset(XElement element)
        {
            var result = new FWVideoAsset();

            result.Id = (string)element.Attribute("id");
            result.CustomId = (string)element.Attribute("customId");
            result.VideoPlayRandom = (string)element.Attribute("videoPlayRandom");

            var adSlotsXml = element.Element("adSlots");
            if (adSlotsXml != null)
            {
                foreach (var adSlotXml in adSlotsXml.Elements("temporalAdSlot"))
                {
                    result.AdSlots.Add(LoadTemporalAdSlot(adSlotXml));
                }
            }

            var eventCallbacksXml = element.Element("eventCallbacks");
            if (eventCallbacksXml != null)
            {
                foreach (var eventCallbackXml in eventCallbacksXml.Elements("eventCallback"))
                {
                    result.EventCallbacks.Add(LoadEventCallback(eventCallbackXml));
                }
            }

            return result;
        }

        internal static FWNonTemporalAdSlot LoadNonTemporalAdSlot(XElement element)
        {
            var result = new FWNonTemporalAdSlot();
            var height = (string)element.Attribute("height");
            if (height != null) result.Height = int.Parse(height);
            var width = (string)element.Attribute("width");
            if (width != null) result.Width = int.Parse(width);
            result.CompatibleDimensions = (string)element.Attribute("compatibleDimensions");
            result.AdUnit = (string)element.Attribute("adUnit");
            result.CustomId = (string)element.Attribute("customId");

            var adsXml = element.Element("selectedAds");
            if (adsXml != null)
            {
                foreach (var adReferenceXml in adsXml.Elements("adReference"))
                {
                    result.SelectedAds.Add(LoadAdReference(adReferenceXml));
                }
            }

            var parametersXml = element.Element("parameters");
            if (parametersXml != null)
            {
                foreach (var parameterXml in parametersXml.Elements("parameter"))
                {
                    result.Parameters.Add(LoadParameter(parameterXml));
                }
            }

            return result;
        }

        internal static FWTemporalAdSlot LoadTemporalAdSlot(XElement element)
        {
            var result = new FWTemporalAdSlot();
            var height = (string)element.Attribute("height");
            if (height != null) result.Height = int.Parse(height);
            var width = (string)element.Attribute("width");
            if (width != null) result.Width = int.Parse(width);
            result.CompatibleDimensions = (string)element.Attribute("compatibleDimensions");
            result.AdUnit = (string)element.Attribute("adUnit");
            result.CustomId = (string)element.Attribute("customId");
            result.Source = (string)element.Attribute("source");
            result.MaxSlotDuration = TimeSpan.FromSeconds(element.GetIntAttribute("maxSlotDuration"));

            var timePositionXml = (string)element.Attribute("timePosition");
            if (timePositionXml != null) result.TimePosition = TimeSpan.FromSeconds(float.Parse(timePositionXml));
            result.TimePositionSequence = element.GetIntAttribute("timePositionSequence");
            result.TimePositionClass = (string)element.Attribute("timePositionClass");

            var adsXml = element.Element("selectedAds");
            if (adsXml != null)
            {
                foreach (var adReferenceXml in adsXml.Elements("adReference"))
                {
                    result.SelectedAds.Add(LoadAdReference(adReferenceXml));
                }
            }

            var eventCallbacksXml = element.Element("eventCallbacks");
            if (eventCallbacksXml != null)
            {
                foreach (var eventCallbackXml in eventCallbacksXml.Elements("eventCallback"))
                {
                    result.EventCallbacks.Add(LoadEventCallback(eventCallbackXml));
                }
            }

            var parametersXml = element.Element("parameters");
            if (parametersXml != null)
            {
                foreach (var parameterXml in parametersXml.Elements("parameter"))
                {
                    result.Parameters.Add(LoadParameter(parameterXml));
                }
            }

            return result;
        }

        internal static FWAdReference LoadAdReference(XElement element)
        {
            var result = new FWAdReference();
            result.CreativeId = (string)element.Attribute("creativeId");
            result.CreativeRenditionId = (string)element.Attribute("creativeRenditionId");
            result.AdId = (string)element.Attribute("adId");
            result.ReplicaId = (string)element.Attribute("replicaId");
            result.SlotEnv = (string)element.Attribute("adSlotEnv");
            result.SlotId = (string)element.Attribute("slotCustomId");

            var eventCallbacksXml = element.Element("eventCallbacks");
            if (eventCallbacksXml != null)
            {
                foreach (var eventCallbackXml in eventCallbacksXml.Elements("eventCallback"))
                {
                    result.EventCallbacks.Add(LoadEventCallback(eventCallbackXml));
                }
            }

            var companionAdsXaml = element.Element("companionAds");
            if (companionAdsXaml != null)
            {
                foreach (var adReferenceXml in companionAdsXaml.Elements("adReference"))
                {
                    result.CompanionAds.Add(LoadAdReference(adReferenceXml));
                }
            }

            var fallbackAdsXaml = element.Element("fallbackAds");
            if (fallbackAdsXaml != null)
            {
                foreach (var adReferenceXml in fallbackAdsXaml.Elements("adReference"))
                {
                    result.FallbackAds.Add(LoadAdReference(adReferenceXml));
                }
            }

            return result;
        }

        internal static FWAdSlot LoadAdSlot(XElement element)
        {
            var result = new FWAdSlot();

            var height = (string)element.Attribute("height");
            if (height != null) result.Height = int.Parse(height);
            var width = (string)element.Attribute("width");
            if (width != null) result.Width = int.Parse(width);
            result.CompatibleDimensions = (string)element.Attribute("compatibleDimensions");
            result.AdUnit = (string)element.Attribute("adUnit");
            result.CustomId = (string)element.Attribute("customId");

            var adsXml = element.Element("selectedAds");
            if (adsXml != null)
            {
                foreach (var adReferenceXml in adsXml.Elements("adReference"))
                {
                    result.SelectedAds.Add(LoadAdReference(adReferenceXml));
                }
            }

            return result;
        }

        /// <summary>
        /// Creates an IAdSource from a VMAP AdSource (required for the AdHandlerPlugin to play the ad).
        /// </summary>
        /// <param name="source">The VMAP AdSource object</param>
        /// <returns>An IAdSource object that can be played by the AdHandlerPlugin. Returns null if insufficient data is available.</returns>
        public static AdDocumentPayload GetAdDocumentPayload(FWTemporalAdSlot adSlot, FWAdResponse adResponse)
        {
            var payload = new AdDocumentPayload();
            var adPod = new AdPod();
            payload.AdPods.Add(adPod);
            foreach (var adReference in adSlot.SelectedAds)
            {
                var ad = CreateAd(adResponse, adReference);
                adPod.Ads.Add(ad);

                foreach (var fallbackAdReference in adReference.FallbackAds)
                {
                    var fallbackAd = CreateAd(adResponse, fallbackAdReference);
                    ad.FallbackAds.Add(fallbackAd);
                }
            }
            return payload;
        }

        private static Ad CreateAd(FWAdResponse adResponse, FWAdReference adReference)
        {
            var ad = adResponse.Ads.FirstOrDefault(a => a.Id == adReference.AdId);
            if (ad != null)
            {
                var linearCreative = CreateLinearAd(ad, adReference);

                foreach (var companionAdReference in adReference.CompanionAds)
                {
                    var companionAd = adResponse.Ads.FirstOrDefault(a => a.Id == companionAdReference.AdId);
                    var companionCreative = CreateCompanionAds(companionAd, companionAdReference);
                    companionCreative.Sequence = 1;
                    linearCreative.Creatives.Add(companionCreative);
                }

                return linearCreative;
            }
            return null;
        }

        public static CreativeCompanions CreateCompanionAds(FWAd source, FWAdReference reference)
        {
            var result = new CreativeCompanions();

            foreach (var creative in source.Creatives)
            {
                var companion = new Companion();
                result.Companions.Add(companion);
                companion.AdSlotId = reference.SlotId ?? string.Empty;

                var rendition = creative.CreativeRenditions.OrderByDescending(cr => cr.Preference).FirstOrDefault(c => c.Asset.MimeType.ToLowerInvariant().StartsWith("image/"));
                if (rendition != null)
                {
                    var asset = rendition.Asset;
                    if (asset != null)
                    {
                        companion.Item = new StaticResource()
                        {
                            Value = new Uri(asset.Url),
                            CreativeType = asset.MimeType
                        };
                        companion.Width = rendition.Width;
                        companion.Height = rendition.Height;
                        companion.Id = asset.Id ?? string.Empty;
                    }
                }

                var allCallbacks = reference.EventCallbacks;
                foreach (var url in allCallbacks.Where(ec => ec.Type == FWCallbackType.Click && !ec.ShowBrowser).SelectMany(ec => ec.GetUrls()))
                {
                    companion.ClickTracking.Add(url);
                }

                var clickUrl = allCallbacks.Where(ec => ec.Type == FWCallbackType.Click && ec.ShowBrowser).SelectMany(ec => ec.GetUrls()).FirstOrDefault();
                if (clickUrl != null)
                {
                    companion.ClickThrough = new Uri(clickUrl);
                }
            }
            return result;
        }

        static Ad CreateLinearAd(FWAd source, FWAdReference reference)
        {
            var ad = new Ad();

            var allCallbacks = reference.EventCallbacks;
            foreach (var url in allCallbacks.Where(ec => ec.Type == FWCallbackType.Impression && ec.Name == FWEventCallback.DefaultImpression).SelectMany(ec => ec.GetUrls()))
            {
                ad.Impressions.Add(url);
            }

            int index = 0;
            foreach (var creative in source.Creatives)
            {
                index++;
                var linear = new CreativeLinear();
                linear.Duration = creative.Duration;
                linear.Sequence = index;

                foreach (var rendition in creative.CreativeRenditions)
                {
                    if (rendition.Asset != null)
                    {
                        var mediaFile = CreateMediaFile(creative, rendition, rendition.Asset);
                        mediaFile.Ranking = (int)rendition.Preference + 1; // add one to indicate this is preferred over "OtherAssets"
                        linear.MediaFiles.Add(mediaFile);
                    }

                    foreach (var asset in rendition.OtherAssets)
                    {
                        var mediaFile = CreateMediaFile(creative, rendition, asset);
                        mediaFile.Ranking = (int)rendition.Preference;
                        linear.MediaFiles.Add(mediaFile);
                    }
                }

                // generate callback urls from one base url
                foreach (var eventCallback in allCallbacks.Where(ec => ec.Type == FWCallbackType.Generic))
                {
                    foreach (var url in eventCallback.GetUrls())
                    {
                        var baseUrl = url + string.Format("&metr={0}", FreeWheelFactory.GetSupportedMetrics());

                        // quartile events
                        var quartileUrl = baseUrl + "&ct=[LASTQUARTILE]&et=i"; // [LASTQUARTILE] will get replaced by the VPAID controller
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.FirstQuartile, Value = quartileUrl + "&cn=firstQuartile" });
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Midpoint, Value = quartileUrl + "&cn=midPoint" });
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.ThirdQuartile, Value = quartileUrl + "&cn=thirdQuartile" });
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Complete, Value = quartileUrl + "&cn=complete" });

                        // advanced metrics
                        var advancedUrl = baseUrl + "&et=s";
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Mute, Value = advancedUrl + "&cn=_mute" });
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Unmute, Value = advancedUrl + "&cn=_un-mute" });
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Collapse, Value = advancedUrl + "&cn=_collapse" });
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Expand, Value = advancedUrl + "&cn=_expand" });
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Pause, Value = advancedUrl + "&cn=_pause" });
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Resume, Value = advancedUrl + "&cn=_resume" });
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Rewind, Value = advancedUrl + "&cn=_rewind" });
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.AcceptInvitation, Value = advancedUrl + "&cn=_accept-invitation" });
                        linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Close, Value = advancedUrl + "&cn=_close" });
                        //linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Minimize, Value = advancedUrl + "&cn=_minimize" });
                    }
                }

                // generate callback urls from one base url
                foreach (var eventCallback in allCallbacks.Where(ec => ec.Type == FWCallbackType.Impression))
                {
                    foreach (var url in eventCallback.GetUrls())
                    {
                        switch (eventCallback.Name.ToLower())
                        {
                            case "start":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Start, Value = url });
                                break;
                            case "firstquartile":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.FirstQuartile, Value = url });
                                break;
                            case "midpoint":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Midpoint, Value = url });
                                break;
                            case "thirdquartile":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.ThirdQuartile, Value = url });
                                break;
                            case "complete":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Complete, Value = url });
                                break;
                        }
                    }
                }

                // generate callback urls from one base url
                foreach (var eventCallback in allCallbacks.Where(ec => ec.Type == FWCallbackType.Standard))
                {
                    foreach (var url in eventCallback.GetUrls())
                    {
                        switch (eventCallback.Name.Replace("-", "").ToLower())
                        { 
                            case "_creativeview":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.CreativeView, Value = url });
                                break;
                            case "_mute":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Mute, Value = url });
                                break;
                            case "_unmute":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Unmute, Value = url });
                                break;
                            case "_pause":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Pause, Value = url });
                                break;
                            case "_rewind":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Rewind, Value = url });
                                break;
                            case "_resume":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Resume, Value = url });
                                break;
                            case "_fullscreen":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Fullscreen, Value = url });
                                break;
                            case "_exitfullscreen":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.ExitFullscreen, Value = url });
                                break;
                            case "_expand":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Expand, Value = url });
                                break;
                            case "_collapse":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Collapse, Value = url });
                                break;
                            case "_acceptinvitation":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.AcceptInvitation, Value = url });
                                break;
                            case "_close":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Close, Value = url });
                                break;
                            case "_skip":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Skip, Value = url });
                                break;
                            case "_progress":
                                linear.TrackingEvents.Add(new TrackingEvent() { Type = TrackingType.Progress, Value = url });
                                break;
                        }
                    }
                }

                foreach (var url in allCallbacks.Where(ec => ec.Type == FWCallbackType.Click && !ec.ShowBrowser).SelectMany(ec => ec.GetUrls()))
                {
                    linear.ClickTracking.Add(url);
                }

                var clickUrl = allCallbacks.Where(ec => ec.Type == FWCallbackType.Click && ec.ShowBrowser).SelectMany(ec => ec.GetUrls()).FirstOrDefault();
                if (clickUrl != null)
                {
                    linear.ClickThrough = new Uri(clickUrl);
                }

                ad.Creatives.Add(linear);
            }
            return ad;
        }

        private static MediaFile CreateMediaFile(FWCreative creative, FWCreativeRendition rendition, FWAsset asset)
        {
            var mediaFile = new MediaFile();

            mediaFile.Value = new Uri(asset.Url);
            mediaFile.Width = rendition.Width;
            mediaFile.Height = rendition.Height;
            mediaFile.Codec = rendition.ContentType ?? asset.ContentType;
            mediaFile.Type = asset.MimeType;
            mediaFile.Id = asset.Id;
            mediaFile.Bitrate = (long)(asset.Bytes / creative.Duration.GetValueOrDefault(TimeSpan.FromSeconds(30)).TotalSeconds / 1000 * 8);

            return mediaFile;
        }

        public static IEnumerable<ICompanionSource> GetNonTemporalCompanions(FWAdResponse adResponse)
        {
            foreach (var adSlot in adResponse.SiteSection.AdSlots.Concat(adResponse.SiteSection.VideoPlayer.AdSlots))
            {
                foreach (var adReference in adSlot.SelectedAds)
                {
                    var companionAd = adResponse.Ads.FirstOrDefault(a => a.Id == adReference.AdId);
                    var companionCreatives = FreeWheelFactory.CreateCompanionAds(companionAd, adReference);
                    foreach (var companionCreative in companionCreatives.Companions)
                    {
                        yield return companionCreative;
                    }
                }
            }
        }

        public static int GetSupportedMetrics()
        {
            // everything but minimize is supported
            return quartile + midPoint + complete + mute + collapse + pause_resume + rewind + accept_invitation + close;
        }

        const int quartile = 1;
        const int midPoint = 2;
        const int complete = 4;
        const int mute = 8;
        const int collapse = 16;
        const int pause_resume = 32;
        const int rewind = 64;
        const int accept_invitation = 128;
        const int close = 256;
        const int minimize = 512;
    }
}
