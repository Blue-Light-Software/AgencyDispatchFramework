using System;

namespace AgencyDispatchFramework.Dispatching
{
    public class EventDescription : ISpawnable
    {
        /// <summary>
        /// Gets the description text for the <see cref="Event"/>
        /// </summary>
        public string Text { get; protected set; }

        /// <summary>
        /// Gets the probability of this description against the other callout descriptions
        /// </summary>
        public int Probability { get; }

        /// <summary>
        /// Creates a new instance of <see cref="EventDescription"/>
        /// </summary>
        /// <param name="probabilty"></param>
        /// <param name="description"></param>
        public EventDescription(int probabilty, string description)
        {
            Probability = probabilty;
            Text = description ?? throw new ArgumentNullException(nameof(description));
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
