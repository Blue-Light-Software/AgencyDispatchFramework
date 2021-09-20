using static AgencyDispatchFramework.Dispatching.RadioMessage;

namespace AgencyDispatchFramework.Dispatching
{
    public class RadioMessageMeta
    {
        /// <summary>
        /// Gets or sets the priority of the message
        /// </summary>
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;

        /// <summary>
        /// Gets the scanner audio to be played
        /// </summary>
        public string AudioString { get; set; }

        /// <summary>
        /// Indicates whether prefix the ScannerAudioString with the player's CallSign
        /// </summary>
        public bool PrefixCallSign { get; set; }

        /// <summary>
        /// Indicates whether to suffix the ScannerAudioString with the Callout Location
        /// </summary>
        public bool UsePosition { get; set; }
    }
}
