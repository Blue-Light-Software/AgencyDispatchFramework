namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// A series of flags that help describe a <see cref="RoadShoulder"/> or a <see cref="RoadPosition"/>. 
    /// These flags can be used used to filter locations for a <see cref="Scripting.Callouts.CalloutScenario"/> 
    /// when a call is created
    /// </summary>
    public enum RoadFlags
    {
        /// <summary>
        /// Describes the location as being along an unpaved road
        /// </summary>
        /// <remarks>Intended location types: RoadShoulder, Residence, Store</remarks>
        DirtRoad,

        /// <summary>
        /// Describes the location as being along a one way road
        /// </summary>
        /// <remarks>Intended location types: RoadShoulder, Residence, Store</remarks>
        OneWayRoad,

        /// <summary>
        /// Describes the location as being along a 2 lane road
        /// </summary>
        /// <remarks>Intended location types: RoadShoulderResidence, Store</remarks>
        TwoLaneRoad,

        /// <summary>
        /// Describes the location as being along a 3 lane road
        /// </summary>
        /// <remarks>Intended location types: RoadShoulderResidence, Store</remarks>
        ThreeLaneRoad,

        /// <summary>
        /// Describes the location as being along a 2 lane road, having a 3rd center turn lane
        /// </summary>
        /// <remarks>Intended location types: RoadShoulderResidence, Store</remarks>
        ThreeLaneCenterTurnRoad,

        /// <summary>
        /// Describes the location as being along a 4 lane road
        /// </summary>
        /// <remarks>Intended location types: RoadShoulderResidence, Store</remarks>
        FourLaneRoad,

        /// <summary>
        /// Describes the location as being along a 5 lane road
        /// </summary>
        /// <remarks>Intended location types: RoadShoulderResidence, Store</remarks>
        FiveLaneRoad,

        /// <summary>
        /// Describes the location as being along a 4 lane road, having a 5th center turn lane
        /// </summary>
        /// <remarks>Intended location types: RoadShoulderResidence, Store</remarks>
        FiveLaneCenterTurnRoad,

        /// <summary>
        /// Describes the location as being along a 6 lane road
        /// </summary>
        /// <remarks>Intended location types: RoadShoulderResidence, Store</remarks>
        SixLaneRoad,

        /// <summary>
        /// Describes a road with a dotted center line, allowing passing into on coming traffic
        /// </summary>
        DottedCenterLane,

        /// <summary>
        /// Describes the location as being on the interstate
        /// </summary>
        OnInterstate,

        /// <summary>
        /// Describes the location as being on the an interstate freeway ramp
        /// </summary>
        OnInterstateRamp,

        /// <summary>
        /// Describes the location as being on a bridge
        /// </summary>
        OnBridge,

        /// <summary>
        /// Describes the location as being inside of a tunnel
        /// </summary>
        InsideTunnel,

        /// <summary>
        /// Describes a location as being along a road with driveways
        /// </summary>
        DrivewaysLeft,

        /// <summary>
        /// Describes a location as being along a road with driveways
        /// </summary>
        DrivewaysRight,

        /// <summary>
        /// Describes a location as being along a road with businesses on the left side
        /// </summary>
        BusinessesLeft,

        /// <summary>
        /// Describes a location as being along a road with businesses on the right side
        /// </summary>
        BusinessesRight,

        /// <summary>
        /// Describes a location as being a Gas station along the freeway
        /// </summary>
        FreewayGasStation,

        /// <summary>
        /// Describes the location as being before a lighted intersection
        /// </summary>
        /// <remarks>
        /// Intended location types: RoadShoulder
        /// </remarks>
        /// <example>
        /// Rear end collision just before the light (light was red, someone not paying attention).
        /// </example>
        BeforeLightedIntersection,

        /// <summary>
        /// Describes the location as being after a lighted intersection
        /// </summary>
        /// <remarks>Intended location types: RoadShoulder</remarks>
        AfterLightedIntersection,

        /// <summary>
        /// Describes the location as being before an intersection with stop signs on all sides
        /// </summary>
        /// <remarks>Intended location types: RoadShoulder</remarks>
        /// <example>
        /// Rear end collision just before the stop sign (someone not paying attention).
        /// </example>
        BeforeStopIntersection,

        /// <summary>
        /// Describes the location as being after an intersection with stop signs
        /// </summary>
        /// <remarks>Intended location types: RoadShoulder</remarks>
        AfterStopIntersection,

        /// <summary>
        /// Describes the location as being near an intersection with stop signs only
        /// on the intersecting road (no stop this direction).
        /// </summary>
        /// <remarks>Intended location types: RoadShoulder</remarks>
        BeforeNoStopIntersection,

        /// <summary>
        /// Describes the location as being after an intersection with stop signs only
        /// on the intersecting road (no stop this direction).
        /// </summary>
        /// <remarks>Intended location types: RoadShoulder</remarks>
        AfterNoStopIntersection,

        /// <summary>
        /// Describes the location as being on the side of a interstate freeway just before an on ramp
        /// </summary>
        /// <remarks>Intended location types: RoadShoulder</remarks>
        BeforeInterstateOnRamp,

        /// <summary>
        /// Describes the location as being on the side of a interstate freeway after an on ramp
        /// </summary>
        /// <remarks>Intended location types: RoadShoulder</remarks>
        AfterInterstateOnRamp,

        /// <summary>
        /// Describes the location as being on the side of a interstate freeway just before an off ramp
        /// </summary>
        /// <remarks>Intended location types: RoadShoulder</remarks>
        BeforeInterstateOffRamp,

        /// <summary>
        /// Describes the location as being on the side of a interstate freeway after an off ramp
        /// </summary>
        /// <remarks>Intended location types: RoadShoulder</remarks>
        AfterInterstateOffRamp,
    }
}
