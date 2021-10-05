using AgencyDispatchFramework.Dispatching;
using LSPD_First_Response.Mod.Callouts;
using System;
using System.IO;
using System.Xml;

namespace AgencyDispatchFramework.Scripting.Callouts
{
    /// <summary>
    /// Provides a base for all callouts withing ADF. This base class will process
    /// all the dispatch magic related to calls.
    /// </summary>
    internal abstract class AgencyCallout : Callout, IEventController
    {
        /// <summary>
        /// Stores the current <see cref="Dispatching.Event"/>
        /// </summary>
        public ActiveEvent Event { get; set; }

        /// <summary>
        /// Gets the <see cref="Callout.ScriptInfo.Name"/>
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public abstract PriorityCall Call { get; protected set; }

        /// <summary>
        /// Loads an xml file and returns the XML document back as an object
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        protected static XmlDocument LoadScenarioFile(params string[] paths)
        {
            // Create file path
            string path = Main.FrameworkFolderPath;
            foreach (string p in paths)
                path = Path.Combine(path, p);

            // Ensure file exists
            if (File.Exists(path))
            {
                // Load XML document
                XmlDocument document = new XmlDocument();
                using (var file = new FileStream(path, FileMode.Open))
                {
                    document.Load(file);
                }

                return document;
            }

            throw new Exception($"[ERROR] AgencyCalloutsPlus: Scenario file does not exist: '{path}'");
        }

        /// <summary>
        /// Attempts to spawn a <see cref="CalloutScenarioMeta"/> based on probability. If no
        /// <see cref="CalloutScenarioMeta"/> can be spawned, the error is logged automatically.
        /// </summary>
        /// <returns>returns a <see cref="CalloutScenarioMeta"/> on success, or null otherwise</returns>
        internal static XmlNode LoadScenarioNode(EventScenarioMeta info)
        {
            // Remove name prefix
            var folderName = info.ControllerName.Replace("AgencyCallout.", "");

            // Load the CalloutMeta
            var document = LoadScenarioFile("Callouts", folderName, "CalloutMeta.xml");

            // Return the Scenario node
            return document.DocumentElement.SelectSingleNode($"Scenarios/{info.ScenarioName}");
        }

        public override bool OnBeforeCalloutDisplayed()
        {
            // Did the callout do thier ONE AND ONLY TASK???
            if (Call == null)
            {
                Log.Error($"AgencyCallout.OnBeforeCalloutDisplayed(): Call was never set by parent Callout named '{Name}'");
                return false;
            }

            // Store data
            Event = Call.EventHandle;
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            // Did the callout do thier ONE AND ONLY TASK???
            if (Call == null)
                throw new ArgumentNullException(nameof(Call));

            // Tell dispatch
            Dispatch.CalloutAccepted(Call, this);
            
            // Base must be called last!
            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            // Did the callout do thier ONE AND ONLY TASK???
            // If not, its not the end of the world because Dispatch is keeping watch but... 
            // still alert the author
            if (Event == null)
            {
                Log.Error("AgencyCallout.OnCalloutNotAccepted: Unable to clear active call because ActiveCall is null!");
                return;
            }

            // Tell dispatch
            Dispatch.CalloutNotAccepted(Call);

            // Base must be called last!
            base.OnCalloutNotAccepted();
        }

        public override void End()
        {
            Dispatch.RegisterCallComplete(Call);
            base.End();
        }
    }
}
