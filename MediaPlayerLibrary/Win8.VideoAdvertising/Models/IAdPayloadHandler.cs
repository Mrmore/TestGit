﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
#if NETFX_CORE
using Windows.Foundation;
#endif

namespace Microsoft.VideoAdvertising
{
    public interface IAdPayloadHandler
    {
        IPlayer Player { get; set; }
        string[] SupportedTypes { get; }

        event EventHandler<LoadPlayerEventArgs> LoadPlayer;
        event EventHandler<UnloadPlayerEventArgs> UnloadPlayer;
        event EventHandler<ActivateAdUnitEventArgs> ActivateAdUnit;
        event EventHandler<DeactivateAdUnitEventArgs> DeactivateAdUnit;
        event EventHandler<AdFailureEventArgs> AdFailure;
        
#if SILVERLIGHT
        Task PreloadAdAsync(IAdSource adSource, CancellationToken cancellationToken);
        Task PlayAdAsync(IAdSource adSource, TimeSpan? startTimeout, CancellationToken cancellationToken, IProgress<AdStatus> progress);
        Task<bool> CancelAd();
#else
        IAsyncAction PreloadAdAsync(IAdSource adSource);
        IAsyncActionWithProgress<AdStatus> PlayAdAsync(IAdSource adSource, TimeSpan? startTimeout);
        IAsyncOperation<bool> CancelAd();
#endif
    }

    public sealed class AdFailureEventArgs
#if SILVERLIGHT
 : EventArgs
#endif
    {
        public AdFailureEventArgs(IAdSource adSource, Exception error)
        {
            AdSource = adSource;
            Error = error;
        }

        /// <summary>
        /// The ad source associated with the playing ad unit.
        /// </summary>
        public IAdSource AdSource { get; private set; }

        /// <summary>
        /// The error associated with the ad.
        /// </summary>
        public Exception Error { get; private set; }
    }

    public sealed class LoadPlayerEventArgs
#if SILVERLIGHT
 : EventArgs
#endif
    {
        public LoadPlayerEventArgs(ICreativeSource creativeSource)
        {
            CreativeSource = creativeSource;
        }

        public ICreativeSource CreativeSource { get; private set; }
        public IVpaid Player { get; set; }
    }

    public sealed class ActivateAdUnitEventArgs
#if SILVERLIGHT
 : EventArgs
#endif
    {
        public ActivateAdUnitEventArgs(ICreativeSource creativeSource, IVpaid player, object creativeConcept, IAdSource adSource, IEnumerable<ICompanionSource> companions, CompanionAdsRequired suggestedCompanionRules)
        {
            Player = player;
            AdSource = adSource;
            CreativeConcept = creativeConcept;
            CreativeSource = creativeSource;
            Companions = companions;
            SuggestedCompanionRules = suggestedCompanionRules;
        }

        public CompanionAdsRequired SuggestedCompanionRules { get; private set; }
        public IEnumerable<ICompanionSource> Companions { get; private set; }

        /// <summary>
        /// Provides ad unit being played.
        /// </summary>
        public ICreativeSource CreativeSource { get; private set; }

        /// <summary>
        /// The VPaid player responsible for playing the ad unit.
        /// </summary>
        public IVpaid Player { get; private set; }

        /// <summary>
        /// An object that represents the creative concept. There can be multiple active ads per creative concept and multiple creative concepts per ad.
        /// </summary>
        public object CreativeConcept { get; private set; }

        /// <summary>
        /// The ad source associated with the playing ad unit.
        /// </summary>
        public IAdSource AdSource { get; private set; }
    }

    public sealed class DeactivateAdUnitEventArgs
#if SILVERLIGHT
 : EventArgs
#endif
    {
        public DeactivateAdUnitEventArgs(ICreativeSource creativeSource, IVpaid player, object creativeConcept, IAdSource adSource)
        {
            Player = player;
            CreativeSource = creativeSource;
            CreativeConcept = creativeConcept;
            AdSource = adSource;
        }

        public DeactivateAdUnitEventArgs(ICreativeSource creativeSource, IVpaid player, object creativeConcept, IAdSource adSource, Exception error)
            : this(creativeSource, player, creativeConcept, adSource)
        {
            Error = error;
        }

        /// <summary>
        /// Provides ad unit being played.
        /// </summary>
        public ICreativeSource CreativeSource { get; private set; }

        /// <summary>
        /// The VPaid player responsible for playing the ad unit.
        /// </summary>
        public IVpaid Player { get; private set; }

        /// <summary>
        /// An object that represents the creative concept. There can be multiple active ads per creative concept and multiple creative concepts per ad.
        /// </summary>
        public object CreativeConcept { get; private set; }

        /// <summary>
        /// The ad source associated with the playing ad unit.
        /// </summary>
        public IAdSource AdSource { get; private set; }

        /// <summary>
        /// The error that caused the ad to deactivate. Otherwise null.
        /// </summary>
        public Exception Error { get; private set; }
    }

    public sealed class UnloadPlayerEventArgs
#if SILVERLIGHT
 : EventArgs
#endif
    {
        public UnloadPlayerEventArgs(IVpaid player)
        {
            Player = player;
        }

        public IVpaid Player { get; private set; }
    }

    public sealed class NavigationRequestEventArgs
#if SILVERLIGHT
 : EventArgs
#endif
    {
        public NavigationRequestEventArgs(string url)
        {
            Url = url;
        }

        public string Url { get; private set; }
    }

    public enum AdStatus
    {
        Loading,
        Opening,
        Loaded,
        Playing,
        Complete,
        Unloaded
    }

    public interface IResolveableAdSource : IAdSource
    {
        bool IsLoaded { get; }
#if SILVERLIGHT
        Task LoadPayload(CancellationToken cancellationToken);
#else
        IAsyncAction LoadPayload();
#endif
    }

    /// <summary>
    /// The source of an ad that should be played or preloaded.
    /// </summary>
    public interface IAdSource
    {
        /// <summary>
        /// Gets or sets a key associated with the ad. Used for internal tracking only. 
        /// Can be set to null.
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// Gets or sets the payload for the ad. The type supplied can vary depending on the type of ad it is.
        /// </summary>
        object Payload { get; set; }

        /// <summary>
        /// Gets or sets a type identifier for the ad source. (e.g. "vast").
        /// This helps us choose an AdHandler to process the payload.
        /// </summary>
        string Type { get; set; }

        /// <summary>
        /// Gets or sets if multiple ads are allowed to play as an ad pod. Default is true.
        /// </summary>
        bool AllowMultipleAds { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of redirects that can happen. 
        /// null indicates no limit. The default is null.
        /// </summary>
        int? MaxRedirectDepth { get; set; }
    }
}
