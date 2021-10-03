using System;

namespace AgencyDispatchFramework.Dispatching
{
    public class LAPDStyleCallsign : CallSign
    {
        /// <summary>
        /// Gets the division
        /// </summary>
        public int Division { get; internal set; }

        /// <summary>
        /// Gets the unit name
        /// </summary>
        public string UnitTypeString { get; internal set; }

        /// <summary>
        /// Gets the beat
        /// </summary>
        public int Beat { get; internal set; }

        /// <summary>
        /// Gets the audio string for the dispatch radio
        /// </summary>
        internal string RadioCallSign { get; set; } = String.Empty;

        /// <summary>
        /// Defines the <see cref="CallSignStyle"/>
        /// </summary>
        public override CallSignStyle Style => CallSignStyle.LAPD;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="division"></param>
        /// <param name="phoneticUnitId"></param>
        /// <param name="beat"></param>
        public LAPDStyleCallsign(int division, int phoneticUnitId, int beat)
        {
            // Ensure division ID is in range
            if (!division.InRange(1, 10))
                throw new ArgumentException("Callsign division number out of range", nameof(division));

            // Ensure division ID is in range
            if (!phoneticUnitId.InRange(1, 26))
                throw new ArgumentException("Callsign phoneticUnitId number out of range", nameof(phoneticUnitId));

            // Ensure division ID is in range
            //if (!beat.InRange(1, 24))
                //throw new ArgumentException("Callsign beat number out of range", nameof(beat));

            Division = division;
            UnitTypeString = Dispatch.LAPDphonetic[phoneticUnitId - 1];
            Beat = beat;

            char unit = char.ToUpper(UnitTypeString[0]);
            Value = $"{Division}{unit}-{Beat}";
            RadioCallSign = CreateRadioString();
        }

        /// <summary>
        /// Gets the audio string for the dispatch radio
        /// </summary>
        /// <returns></returns>
        private string CreateRadioString()
        {
            // Pad zero
            var divString = Division.ToString("D2");

            if (Beat < 25)
            {
                string beatString = Beat.ToString("D2");
                return $"DIV_{divString} {UnitTypeString} BEAT_{beatString}";
            }
            else
            {
                string beatString = "";
                foreach (char e in Beat.ToString())
                {
                    beatString += $" BEAT_{e}";
                }

                return $"DIV_{divString} {UnitTypeString}{beatString}";
            }
        }

        /// <summary>
        /// Gets the audio string for the dispatch radio
        /// </summary>
        /// <returns></returns>
        public override string GetRadioString()
        {
            return RadioCallSign;
        }

        public override string ToString() => Value;
    }
}
