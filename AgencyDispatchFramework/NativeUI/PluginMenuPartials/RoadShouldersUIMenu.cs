using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace AgencyDispatchFramework.NativeUI
{
    internal partial class DeveloperPluginMenu
    {
        private UIMenuItem RoadShoulderCreateButton { get; set; }

        private UIMenuItem RoadShoulderLoadBlipsButton { get; set; }

        private UIMenuItem RoadShoulderClearBlipsButton { get; set; }

        private SpawnPoint RoadShoulderLocation { get; set; }

        private UIMenuNumericScrollerItem<int> RoadShoulderSpeedButton { get; set; }

        private UIMenuListItem RoadShoulderPostalButton { get; set; }

        private UIMenuListItem RoadShoulderZoneButton { get; set; }

        private UIMenuItem RoadShoulderStreetButton { get; set; }

        private UIMenuItem RoadShoulderHintButton { get; set; }

        private UIMenuItem RoadShoulderFlagsButton { get; set; }

        private Dictionary<RoadFlags, UIMenuCheckboxItem> RoadShouldFlagsItems { get; set; }

        private UIMenuItem RoadShoulderBeforeFlagsButton { get; set; }

        private UIMenuListItem RoadShoulderBeforeListButton { get; set; }

        private Dictionary<IntersectionFlags, UIMenuCheckboxItem> BeforeIntersectionItems { get; set; }

        private UIMenuItem RoadShoulderAfterFlagsButton { get; set; }

        private UIMenuListItem RoadShoulderAfterListButton { get; set; }

        private Dictionary<IntersectionFlags, UIMenuCheckboxItem> AfterIntersectionItems { get; set; }

        private UIMenuItem RoadShoulderSaveButton { get; set; }

        private UIMenuItem RoadShoulderSpawnPointsButton { get; set; }

        private Dictionary<RoadShoulderPosition, UIMenuItem<SpawnPoint>> RoadShoulderSpawnPointItems { get; set; }

        /// <summary>
        /// Builds the menu and its buttons
        /// </summary>
        private void BuildRoadShouldersMenu()
        {
            // Create road shoulder ui menu
            RoadUIMenu = new UIMenu(MENU_NAME, "~b~Road Shoulder Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Create road shoulder ui menu
            AddRoadUIMenu = new UIMenu(MENU_NAME, "~b~Add Road Shoulder")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Road Shoulder flags selection menu
            RoadShoulderFlagsUIMenu = new UIMenu(MENU_NAME, "~b~Road Shoulder Flags")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Road shoulder spawn points menu
            RoadShoulderSpawnPointsUIMenu = new UIMenu(MENU_NAME, "~b~Road Shoulder Spawn Points")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // *************************************************
            // RoadShoulder UI Menu
            // *************************************************

            // Setup buttons
            RoadShoulderCreateButton = new UIMenuItem("Add New Location", "Creates a new Road Shoulder location where you are currently");
            RoadShoulderLoadBlipsButton = new UIMenuItem("Load Checkpoints", "Loads checkpoints in the world as well as blips on the map to show all saved locations in this zone");
            RoadShoulderClearBlipsButton = new UIMenuItem("Clear Checkpoints", "Clears all checkpoints and blips loaded by the ~y~Load Checkpoints ~w~option");

            // Button Events
            RoadShoulderCreateButton.Activated += RoadShouldersCreateButton_Activated;
            RoadShoulderLoadBlipsButton.Activated += (s, e) => LoadZoneLocations(LocationsDB.RoadShoulders.Query(), Color.Red);
            RoadShoulderClearBlipsButton.Activated += (s, e) => ClearZoneLocations();

            // Add buttons
            RoadUIMenu.AddItem(RoadShoulderCreateButton);
            RoadUIMenu.AddItem(RoadShoulderLoadBlipsButton);
            RoadUIMenu.AddItem(RoadShoulderClearBlipsButton);

            // Bind Buttons
            RoadUIMenu.BindMenuToItem(AddRoadUIMenu, RoadShoulderCreateButton);

            // *************************************************
            // Add RoadShoulder UI Menu
            // *************************************************

            // Setup Buttons
            RoadShoulderStreetButton = new UIMenuItem("Street Name", "");
            RoadShoulderHintButton = new UIMenuItem("Location Hint", "");
            RoadShoulderSpeedButton = new UIMenuNumericScrollerItem<int>("Speed Limit", "Sets the speed limit of this road", 10, 80, 5);
            RoadShoulderZoneButton = new UIMenuListItem("Zone", "Selects the jurisdictional zone");
            RoadShoulderPostalButton = new UIMenuListItem("Postal", "The current location's Postal Code.");
            RoadShoulderSpawnPointsButton = new UIMenuItem("Spawn Points", "Sets safe spawn points for ped groups");
            RoadShoulderFlagsButton = new UIMenuItem("Road Shoulder Flags", "Open the RoadShoulder flags menu.");
            RoadShoulderSaveButton = new UIMenuItem("Save", "Saves the current location data to the XML file");

            // Button events
            RoadShoulderStreetButton.Activated += DispayKeyboard_SetDescription;
            RoadShoulderHintButton.Activated += DispayKeyboard_SetDescription;
            RoadShoulderSaveButton.Activated += RoadShoulderSaveButton_Activated;

            // Add Buttons
            AddRoadUIMenu.AddItem(RoadShoulderStreetButton);
            AddRoadUIMenu.AddItem(RoadShoulderHintButton);
            AddRoadUIMenu.AddItem(RoadShoulderSpeedButton);
            AddRoadUIMenu.AddItem(RoadShoulderZoneButton);
            AddRoadUIMenu.AddItem(RoadShoulderPostalButton);
            AddRoadUIMenu.AddItem(RoadShoulderSpawnPointsButton);
            AddRoadUIMenu.AddItem(RoadShoulderFlagsButton);
            AddRoadUIMenu.AddItem(RoadShoulderSaveButton);

            // Bind buttons
            AddRoadUIMenu.BindMenuToItem(RoadShoulderFlagsUIMenu, RoadShoulderFlagsButton);
            AddRoadUIMenu.BindMenuToItem(RoadShoulderSpawnPointsUIMenu, RoadShoulderSpawnPointsButton);

            // *************************************************
            // Intersection Flags
            // *************************************************
            RoadShoulderBeforeFlagsUIMenu = new UIMenu(MENU_NAME, "~b~Before Intersection Flags")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            RoadShoulderAfterFlagsUIMenu = new UIMenu(MENU_NAME, "~b~After Intersection Flags")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Create Buttons
            RoadShoulderBeforeListButton = new UIMenuListItem("Direction", "The direction of the ajoining road (only applies to ~y~Three Way intersections)");
            RoadShoulderAfterListButton = new UIMenuListItem("Direction", "The direction of the ajoining road (only applies to ~y~Three Way intersections)");

            // Add directions
            foreach (RelativeDirection direction in Enum.GetValues(typeof(RelativeDirection)))
            {
                var name = Enum.GetName(typeof(RelativeDirection), direction);
                RoadShoulderBeforeListButton.Collection.Add(direction, name);
                RoadShoulderAfterListButton.Collection.Add(direction, name);
            }

            // Add buttons to the menu
            RoadShoulderBeforeFlagsUIMenu.AddItem(RoadShoulderBeforeListButton);
            RoadShoulderAfterFlagsUIMenu.AddItem(RoadShoulderAfterListButton);

            // Add road shoulder intersection flags
            BeforeIntersectionItems = new Dictionary<IntersectionFlags, UIMenuCheckboxItem>();
            AfterIntersectionItems = new Dictionary<IntersectionFlags, UIMenuCheckboxItem>();
            foreach (IntersectionFlags flag in Enum.GetValues(typeof(IntersectionFlags)))
            {
                var name = Enum.GetName(typeof(IntersectionFlags), flag);
                var cb = new UIMenuCheckboxItem(name, false);
                BeforeIntersectionItems.Add(flag, cb);
                RoadShoulderBeforeFlagsUIMenu.AddItem(cb);

                cb = new UIMenuCheckboxItem(name, false);
                AfterIntersectionItems.Add(flag, cb);
                RoadShoulderAfterFlagsUIMenu.AddItem(cb);
            }

            // Bind buttons
            RoadShoulderBeforeFlagsButton = new UIMenuItem("Before Intersection Flags");
            RoadShoulderBeforeFlagsButton.LeftBadge = UIMenuItem.BadgeStyle.Car;
            RoadShoulderFlagsUIMenu.AddItem(RoadShoulderBeforeFlagsButton);
            RoadShoulderFlagsUIMenu.BindMenuToItem(RoadShoulderBeforeFlagsUIMenu, RoadShoulderBeforeFlagsButton);

            RoadShoulderAfterFlagsButton = new UIMenuItem("After Intersection Flags");
            RoadShoulderAfterFlagsButton.LeftBadge = UIMenuItem.BadgeStyle.Car;
            RoadShoulderFlagsUIMenu.AddItem(RoadShoulderAfterFlagsButton);
            RoadShoulderFlagsUIMenu.BindMenuToItem(RoadShoulderAfterFlagsUIMenu, RoadShoulderAfterFlagsButton);

            // Add positions
            RoadShoulderSpawnPointItems = new Dictionary<RoadShoulderPosition, UIMenuItem<SpawnPoint>>();
            foreach (RoadShoulderPosition flag in Enum.GetValues(typeof(RoadShoulderPosition)))
            {
                var name = Enum.GetName(typeof(RoadShoulderPosition), flag);
                var item = new UIMenuItem<SpawnPoint>(null, name, "Activate to set position. ~y~Ensure there is proper space to spawn a group of Peds at this location");
                item.Activated += RoadShoulderSpawnPointButton_Activated;
                RoadShoulderSpawnPointItems.Add(flag, item);

                // Add button
                RoadShoulderSpawnPointsUIMenu.AddItem(item);
            }

            // Add road shoulder flags list
            RoadShouldFlagsItems = new Dictionary<RoadFlags, UIMenuCheckboxItem>();
            foreach (RoadFlags flag in Enum.GetValues(typeof(RoadFlags)))
            {
                var name = Enum.GetName(typeof(RoadFlags), flag);
                var cb = new UIMenuCheckboxItem(name, false);
                RoadShouldFlagsItems.Add(flag, cb);

                // Add button
                RoadShoulderFlagsUIMenu.AddItem(cb);
            }

            // Register for events
            AddRoadUIMenu.OnMenuChange += AddRoadShoulderUIMenu_OnMenuChange;
            RoadShoulderFlagsUIMenu.OnMenuChange += RoadShoulderFlagsUIMenu_OnMenuChange;
        }

        /// <summary>
        /// Method called when the "Create New Road Shoulder" button is clicked.
        /// Clears all prior data.
        /// </summary>
        private void RoadShouldersCreateButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            //
            // Reset everything!
            //
            if (NewLocationCheckpoint != null)
            {
                NewLocationCheckpoint.Dispose();
                NewLocationCheckpoint = null;
            }

            // Grab player location
            var pos = Rage.Game.LocalPlayer.Character.Position;
            var cpPos = new Vector3(pos.X, pos.Y, pos.Z - ZCorrection);
            var heading = Rage.Game.LocalPlayer.Character.Heading;
            RoadShoulderLocation = new SpawnPoint(cpPos, heading);

            // Reset road shoulder flags
            foreach (var cb in RoadShouldFlagsItems.Values)
            {
                cb.Checked = false;
            }

            // Reset spawn points
            foreach (UIMenuItem<SpawnPoint> item in RoadShoulderSpawnPointItems.Values)
            {
                item.Tag = null;
                item.RightBadge = UIMenuItem.BadgeStyle.None;
            }

            // Reset intersection flags
            foreach (var cb in BeforeIntersectionItems)
            {
                cb.Value.Checked = false;
                AfterIntersectionItems[cb.Key].Checked = false;
            }
            RoadShoulderBeforeListButton.Index = 0;
            RoadShoulderAfterListButton.Index = 0;

            // Create checkpoint at the player location
            NewLocationCheckpoint = GameWorld.CreateCheckpoint(cpPos, Color.Red);

            // Get current Postal
            RoadShoulderPostalButton.Collection.Clear();
            var postal = Postal.FromVector(pos);
            RoadShoulderPostalButton.Collection.Add(postal, postal.Code.ToString());

            // Add Zones
            RoadShoulderZoneButton.Collection.Clear();
            RoadShoulderZoneButton.Collection.Add(GameWorld.GetZoneNameAtLocation(pos));
            RoadShoulderZoneButton.Collection.Add("HIGHWAY");

            // Set street name default
            var streetName = GameWorld.GetStreetNameAtLocation(pos, out string crossingRoad);
            if (!String.IsNullOrEmpty(streetName))
            {
                RoadShoulderStreetButton.Description = streetName;
                RoadShoulderStreetButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
            }
            else
            {
                RoadShoulderStreetButton.Description = "";
                RoadShoulderStreetButton.RightBadge = UIMenuItem.BadgeStyle.None;
            }

            // Crossing road
            if (!String.IsNullOrEmpty(crossingRoad))
            {
                RoadShoulderHintButton.Description = $"near {crossingRoad}";
                RoadShoulderHintButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
            }
            else
            {
                // Reset ticks
                RoadShoulderHintButton.RightBadge = UIMenuItem.BadgeStyle.None;
            }

            // Reset
            RoadShoulderFlagsButton.RightBadge = UIMenuItem.BadgeStyle.None;
            RoadShoulderSpawnPointsButton.RightBadge = UIMenuItem.BadgeStyle.None;

            // Enable button
            RoadShoulderSaveButton.Enabled = true;
        }

        /// <summary>
        /// Method called when a residece "Spawnpoint" button is clicked on the Road Shoulder Spawn Points UI menu
        /// </summary>
        private void RoadShoulderSpawnPointButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            Checkpoint handle;
            var pos = GamePed.Player.Position;
            var heading = GamePed.Player.Heading;
            var value = (RoadShoulderPosition)Enum.Parse(typeof(RoadShoulderPosition), selectedItem.Text);
            int index = (int)value;
            var menuItem = (UIMenuItem<SpawnPoint>)selectedItem;

            // Check, do we have a check point already for this position?
            if (SpawnPointHandles.ContainsKey(index))
            {
                handle = SpawnPointHandles[index];
                handle.Dispose();
            }

            // Create new checkpoint !!important, need to subtract 2 from the Z since checkpoints spawn at waist level
            var cpPos = new Vector3(pos.X, pos.Y, pos.Z - ZCorrection);
            handle = GameWorld.CreateCheckpoint(cpPos, Color.Yellow, radius: 5f);
            if (SpawnPointHandles.ContainsKey(index))
                SpawnPointHandles[index] = handle;
            else
                SpawnPointHandles.Add(index, handle);

            // Create spawn point
            menuItem.Tag = new SpawnPoint(cpPos, heading);
            menuItem.RightBadge = UIMenuItem.BadgeStyle.Tick;

            // Are we complete
            bool complete = true;
            foreach (UIMenuItem<SpawnPoint> item in RoadShoulderSpawnPointItems.Values)
            {
                if (item.Tag == null)
                {
                    complete = false;
                    break;
                }
            }

            // Signal to the player
            if (complete)
            {
                RoadShoulderSpawnPointsButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
            }
        }

        /// <summary>
        /// Method called when the "Save" button is clicked on the Road Shoulder UI menu
        /// </summary>
        private void RoadShoulderSaveButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Disable button to prevent spam clicking!
            RoadShoulderSaveButton.Enabled = false;

            // Ensure everything is done
            var requiredItems = new[] { RoadShoulderStreetButton, RoadShoulderSpawnPointsButton, RoadShoulderFlagsButton };
            foreach (var item in requiredItems)
            {
                if (item.RightBadge != UIMenuItem.BadgeStyle.Tick)
                {
                    // Display notification to the player
                    Rage.Game.DisplayNotification(
                        "3dtextures",
                        "mpgroundlogo_cops",
                        "Agency Dispatch Framework",
                        "Add Road Shoulder",
                        $"~o~Location does not have all required parameters set"
                    );
                    RoadShoulderSaveButton.Enabled = true;
                    return;
                }
            }

            // Be nice and prevent locking up
            GameFiber.Yield();

            // Grab zone instance
            string zoneName = RoadShoulderZoneButton.SelectedValue.ToString();
            var zone = LocationsDB.WorldZones.FindOne(x => x.ScriptName == zoneName);
            if (zone == null)
            {
                // Display notification to the player
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "Add Road Shoulder",
                    $"~rSave Failed: ~o~Unable to find zone in the locations database!"
                );
                return;
            }

            // Wrap to prevent crashing
            try
            {
                // Create new shoulder object
                var shoulder = new RoadShoulder()
                {
                    Position = RoadShoulderLocation.Position,
                    Heading = RoadShoulderLocation.Heading,
                    SpeedLimit = RoadShoulderSpeedButton.Value,
                    StreetName = RoadShoulderStreetButton.Description,
                    Hint = RoadShoulderHintButton.Description,
                    Flags = RoadShouldFlagsItems.Where(x => x.Value.Checked).Select(x => x.Key).ToList(),
                    BeforeIntersectionFlags = BeforeIntersectionItems.Where(x => x.Value.Checked).Select(x => x.Key).ToList(),
                    BeforeIntersectionDirection = (RelativeDirection)RoadShoulderBeforeListButton.SelectedValue,
                    AfterIntersectionFlags = AfterIntersectionItems.Where(x => x.Value.Checked).Select(x => x.Key).ToList(),
                    AfterIntersectionDirection = (RelativeDirection)RoadShoulderAfterListButton.SelectedValue,
                    SpawnPoints = RoadShoulderSpawnPointItems.ToDictionary(k => k.Key, v => v.Value.Tag),
                    Zone = zone
                };

                // Be nice and prevent locking up
                GameFiber.Yield();

                // Save location in the database
                LocationsDB.RoadShoulders.Insert(shoulder);

                // Display notification to the player
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~b~Add Road Shoulder.",
                    $"~g~Location saved Successfully."
                );
            }
            catch (Exception e)
            {
                Log.Exception(e);

                // Display notification to the player
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~b~Add Road Shoulder.",
                    $"~rSave Failed: ~o~Please check your Game.log!"
                );
            }

            // Go back
            AddRoadUIMenu.GoBack();

            // Are we currently showing checkpoints and blips?
            if (ZoneCheckpoints.Count > 0)
            {
                var pos = RoadShoulderLocation.Position;
                ZoneCheckpoints.Add(GameWorld.CreateCheckpoint(pos, Color.Yellow, forceGround: true));
                ZoneBlips.Add(new Blip(pos) { Color = Color.Red });
            }
        } 
    }
}
