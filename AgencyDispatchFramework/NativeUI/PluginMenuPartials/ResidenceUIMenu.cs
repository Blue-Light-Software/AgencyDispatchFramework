using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Xml;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace AgencyDispatchFramework.NativeUI
{
    internal partial class DeveloperPluginMenu
    {
        private UIMenuItem ResidenceCreateButton { get; set; }

        private UIMenuItem ResidenceLoadBlipsButton { get; set; }

        private UIMenuItem ResidenceClearBlipsButton { get; set; }

        private UIMenuItem ResidencePositionButton { get; set; }

        private UIMenuListItem ResidencePostalButton { get; set; }

        private UIMenuListItem ResidenceZoneButton { get; set; }

        private UIMenuListItem ResidenceClassButton { get; set; }

        private UIMenuItem ResidenceStreetButton { get; set; }

        private UIMenuItem ResidenceNumberButton { get; set; }

        private UIMenuItem ResidenceUnitButton { get; set; }

        private UIMenuItem ResidenceSpawnPointsButton { get; set; }

        private UIMenuItem ResidenceFlagsButton { get; set; }

        private UIMenuItem ResidenceSaveButton { get; set; }

        private Dictionary<ResidenceFlags, UIMenuCheckboxItem> ResidenceFlagsItems { get; set; }

        private Dictionary<ResidencePosition, UIMenuItem<SpawnPoint>> ResidenceSpawnPointItems { get; set; }

        /// <summary>
        /// Builds the menu and its buttons
        /// </summary>
        private void BuildResidencesMenu()
        {
            // Create residence ui menu
            ResidenceUIMenu = new UIMenu(MENU_NAME, "~b~Residence Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };


            // Create add residence ui menu
            AddResidenceUIMenu = new UIMenu(MENU_NAME, "~b~Add Residence")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Flags selection menu
            ResidenceFlagsUIMenu = new UIMenu(MENU_NAME, "~b~Residence Flags")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Residence spawn points ui menu
            ResidenceSpawnPointsUIMenu = new UIMenu(MENU_NAME, "~b~Residence Spawn Points")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // *************************************************
            // Residence UI Menu
            // *************************************************

            // Setup buttons
            ResidenceCreateButton = new UIMenuItem("Add New Location", "Creates a new Residence location where you are currently");
            ResidenceLoadBlipsButton = new UIMenuItem("Load Checkpoints", "Loads checkpoints in the world as well as blips on the map to show all saved locations in this zone");
            ResidenceClearBlipsButton = new UIMenuItem("Clear Checkpoints", "Clears all checkpoints and blips loaded by the ~y~Load Checkpoints ~w~option");

            // Button Events
            ResidenceCreateButton.Activated += ResidenceCreateButton_Activated;
            ResidenceLoadBlipsButton.Activated += (s, e) => LoadZoneLocations(LocationsDB.Residences.Query(), Color.Yellow);
            ResidenceClearBlipsButton.Activated += (s, e) => ClearZoneLocations();

            // Add buttons
            ResidenceUIMenu.AddItem(ResidenceCreateButton);
            ResidenceUIMenu.AddItem(ResidenceLoadBlipsButton);
            ResidenceUIMenu.AddItem(ResidenceClearBlipsButton);

            // Bind Buttons
            ResidenceUIMenu.BindMenuToItem(AddResidenceUIMenu, ResidenceCreateButton);

            // *************************************************
            // Add Residence UI Menu
            // *************************************************

            // Setup Buttons
            ResidencePositionButton = new UIMenuItem("Location", "Sets the street location coordinates for this home. Please stand on the street facing the home, and activate this button.");
            ResidenceNumberButton = new UIMenuItem("Building Number", "");
            ResidenceUnitButton = new UIMenuItem("Room/Unit Number", "");
            ResidenceStreetButton = new UIMenuItem("Street Name", "");
            ResidenceClassButton = new UIMenuListItem("Class", "Sets the social class of this home");
            ResidenceZoneButton = new UIMenuListItem("Zone", "Selects the jurisdictional zone");
            ResidencePostalButton = new UIMenuListItem("Postal", "The current location's Postal Code");
            ResidenceSpawnPointsButton = new UIMenuItem("Spawn Points", "Sets the required spawn points");
            ResidenceFlagsButton = new UIMenuItem("Residence Flags", "Open the Residence flags menu");
            ResidenceSaveButton = new UIMenuItem("Save", "Saves the current residence to the XML file");

            // Button events
            ResidencePositionButton.Activated += ResidencePositionButton_Activated;
            ResidenceNumberButton.Activated += DispayKeyboard_SetDescription;
            ResidenceUnitButton.Activated += DispayKeyboard_SetDescription;
            ResidenceStreetButton.Activated += DispayKeyboard_SetDescription;
            ResidenceSaveButton.Activated += ResidenceSaveButton_Activated;

            // Add Buttons
            AddResidenceUIMenu.AddItem(ResidencePositionButton);
            AddResidenceUIMenu.AddItem(ResidenceNumberButton);
            AddResidenceUIMenu.AddItem(ResidenceUnitButton);
            AddResidenceUIMenu.AddItem(ResidenceStreetButton);
            AddResidenceUIMenu.AddItem(ResidenceClassButton);
            AddResidenceUIMenu.AddItem(ResidenceZoneButton);
            AddResidenceUIMenu.AddItem(ResidencePostalButton);
            AddResidenceUIMenu.AddItem(ResidenceSpawnPointsButton);
            AddResidenceUIMenu.AddItem(ResidenceFlagsButton);
            AddResidenceUIMenu.AddItem(ResidenceSaveButton);

            // Bind buttons
            AddResidenceUIMenu.BindMenuToItem(ResidenceFlagsUIMenu, ResidenceFlagsButton);
            AddResidenceUIMenu.BindMenuToItem(ResidenceSpawnPointsUIMenu, ResidenceSpawnPointsButton);

            // *************************************************
            // Residence Flags
            // *************************************************

            // Add flags
            ResidenceFlagsItems = new Dictionary<ResidenceFlags, UIMenuCheckboxItem>();
            foreach (ResidenceFlags flag in Enum.GetValues(typeof(ResidenceFlags)))
            {
                var name = Enum.GetName(typeof(ResidenceFlags), flag);
                var cb = new UIMenuCheckboxItem(name, false);
                ResidenceFlagsItems.Add(flag, cb);
                ResidenceFlagsUIMenu.AddItem(cb);
            }

            // Add positions
            ResidenceSpawnPointItems = new Dictionary<ResidencePosition, UIMenuItem<SpawnPoint>>();
            foreach (ResidencePosition flag in Enum.GetValues(typeof(ResidencePosition)))
            {
                var name = Enum.GetName(typeof(ResidencePosition), flag);
                var item = new UIMenuItem<SpawnPoint>(null, name, "Activate to set position. Character facing is important");
                item.Activated += ResidenceSpawnPointButton_Activated;
                ResidenceSpawnPointItems.Add(flag, item);

                // Add button
                ResidenceSpawnPointsUIMenu.AddItem(item);
            }

            // Add class flags
            foreach (SocialClass flag in Enum.GetValues(typeof(SocialClass)))
            {
                var name = Enum.GetName(typeof(SocialClass), flag);
                ResidenceClassButton.Collection.Add(name);
            }

            // Register for events
            AddResidenceUIMenu.OnMenuChange += AddResidenceUIMenu_OnMenuChange;
            ResidenceFlagsUIMenu.OnMenuChange += ResidenceFlagsUIMenu_OnMenuChange;
        }

        /// <summary>
        /// Method called when the "Create New Residence" button is clicked.
        /// Clears all prior data.
        /// </summary>
        private void ResidenceCreateButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            //
            // Reset everything!
            //
            if (NewLocationCheckpoint != null)
            {
                NewLocationCheckpoint.Dispose();
                NewLocationCheckpoint = null;
            }

            // Reset flags
            foreach (var cb in ResidenceFlagsItems.Values)
            {
                cb.Checked = false;
            }

            // Reset spawn points
            foreach (UIMenuItem<SpawnPoint> item in ResidenceSpawnPointItems.Values)
            {
                item.Tag = null;
                item.RightBadge = UIMenuItem.BadgeStyle.None;
            }

            // Grab player location
            var pos = Rage.Game.LocalPlayer.Character.Position;

            // Get current Postal
            ResidencePostalButton.Collection.Clear();
            var postal = Postal.FromVector(pos);
            ResidencePostalButton.Collection.Add(postal, postal.Code.ToString());

            // Add Zones
            ResidenceZoneButton.Collection.Clear();
            ResidenceZoneButton.Collection.Add(GameWorld.GetZoneNameAtLocation(pos));

            // Reset buttons
            ResidenceNumberButton.Description = "";
            ResidenceNumberButton.RightBadge = UIMenuItem.BadgeStyle.None;

            ResidenceStreetButton.Description = "";
            ResidenceStreetButton.RightBadge = UIMenuItem.BadgeStyle.None;

            // Reset ticks
            ResidenceUnitButton.Description = "";
            ResidenceUnitButton.RightBadge = UIMenuItem.BadgeStyle.Tick; // Not required

            ResidenceFlagsButton.RightBadge = UIMenuItem.BadgeStyle.None;
            ResidenceSpawnPointsButton.RightBadge = UIMenuItem.BadgeStyle.None;
            ResidencePositionButton.RightBadge = UIMenuItem.BadgeStyle.None;

            // Enable button
            ResidenceSaveButton.Enabled = true;
        }

        private void ResidencePositionButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Delete old handle
            if (NewLocationCheckpoint != null)
            {
                NewLocationCheckpoint.Dispose();
            }

            // Set new location
            NewLocationPosition = new SpawnPoint(GamePed.Player.Position, GamePed.Player.Heading);
            ResidencePositionButton.RightBadge = UIMenuItem.BadgeStyle.Tick;

            // Create checkpoint here
            var pos = GamePed.Player.Position;
            var cpPos = new Vector3(pos.X, pos.Y, pos.Z - ZCorrection);
            NewLocationCheckpoint = GameWorld.CreateCheckpoint(cpPos, Color.Purple);

            // Set street name default
            var streetName = GameWorld.GetStreetNameAtLocation(GamePed.Player.Position);
            if (!String.IsNullOrEmpty(streetName))
            {
                ResidenceStreetButton.Description = streetName;
                ResidenceStreetButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
            }
        }

        /// <summary>
        /// Method called when a residece "Spawnpoint" button is clicked on the Residence Spawn Points UI menu
        /// </summary>
        private void ResidenceSpawnPointButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            Checkpoint checkpoint;
            var pos = GamePed.Player.Position;
            var value = (ResidencePosition)Enum.Parse(typeof(ResidencePosition), selectedItem.Text);
            int index = (int)value;
            var menuItem = (UIMenuItem<SpawnPoint>)selectedItem;

            // Check, do we have a check point already for this position?
            if (SpawnPointHandles.ContainsKey(index))
            {
                checkpoint = SpawnPointHandles[index];
                checkpoint.Dispose();
            }

            // Create new checkpoint !!important, need to subtract 2 from the Z since checkpoints spawn at waist level
            var cpPos = new Vector3(pos.X, pos.Y, pos.Z - ZCorrection);
            checkpoint = GameWorld.CreateCheckpoint(cpPos, GetResidencePositionColor(value), radius: 1f);
            if (SpawnPointHandles.ContainsKey(index))
                SpawnPointHandles[index] = checkpoint;
            else
                SpawnPointHandles.Add(index, checkpoint);

            // Create spawn point
            menuItem.Tag = new SpawnPoint(cpPos, GamePed.Player.Heading);
            menuItem.RightBadge = UIMenuItem.BadgeStyle.Tick;

            // Are we complete
            bool complete = true;
            foreach (UIMenuItem<SpawnPoint> item in ResidenceSpawnPointItems.Values)
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
                ResidenceSpawnPointsButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
            }
        }

        /// <summary>
        /// Method called when the "Save" button is clicked on the Residence UI menu
        /// </summary>
        private void ResidenceSaveButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Disable button to prevent spam clicking!
            ResidenceSaveButton.Enabled = false;

            // Ensure everything is done
            var requiredItems = new[] { ResidenceFlagsButton, ResidenceNumberButton, ResidencePositionButton, ResidenceSpawnPointsButton, ResidenceStreetButton };
            foreach (var item in requiredItems)
            {
                if (item.RightBadge != UIMenuItem.BadgeStyle.Tick)
                {
                    // Display notification to the player
                    Rage.Game.DisplayNotification(
                        "3dtextures",
                        "mpgroundlogo_cops",
                        "Agency Dispatch Framework",
                        "Add Residence",
                        $"~o~Location does not have all required parameters set"
                    );
                    ResidenceSaveButton.Enabled = true;
                    return;
                }
            }

            // @todo Save file
            var pos = NewLocationPosition;
            string zoneName = ResidenceZoneButton.SelectedValue.ToString();

            // Be nice and prevent locking up
            GameFiber.Yield();

            // Grab zone instance
            var zone = LocationsDB.WorldZones.FindOne(x => x.ScriptName == zoneName);
            if (zone == null)
            {
                // Display notification to the player
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "Add Residence",
                    $"~rSave Failed: ~o~Unable to find zone in the locations database!"
                );
                return;
            }

            // Wrap to prevent crashing
            try
            {
                // Create residence object
                var home = new Residence()
                {
                    Position = NewLocationPosition,
                    Heading = NewLocationPosition.Heading,
                    BuildingNumber = ResidenceNumberButton.Description,
                    StreetName = ResidenceStreetButton.Description,
                    UnitId = ResidenceUnitButton.Description,
                    Class = (SocialClass)ResidenceClassButton.SelectedValue,
                    Flags = ResidenceFlagsItems.Where(x => x.Value.Checked).Select(x => x.Key).ToList(),
                    SpawnPoints = ResidenceSpawnPointItems.ToDictionary(k => k.Key, v => v.Value.Tag),
                    Zone = zone
                };

                // Be nice and prevent locking up
                GameFiber.Yield();

                // Save location in the database
                LocationsDB.Residences.Insert(home);

                // Display notification to the player
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~b~Add Residence",
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
                    "~b~Add Residence",
                    $"~rSave Failed: ~o~Please check your Game.log!"
                );
            }

            // Go back
            AddResidenceUIMenu.GoBack();

            // Are we currently showing checkpoints and blips?
            if (ZoneCheckpoints.Count > 0)
            {
                ZoneCheckpoints.Add(GameWorld.CreateCheckpoint(pos, Color.Yellow, forceGround: true));
                ZoneBlips.Add(new Blip(pos) { Color = Color.Red });
            }
        }

        private Color GetResidencePositionColor(ResidencePosition value)
        {
            switch (value)
            {
                case ResidencePosition.BackDoorPed:
                case ResidencePosition.FrontDoorPed1:
                case ResidencePosition.SidewalkPed:
                    return Color.Yellow;
                case ResidencePosition.FrontDoorPolicePed1:
                case ResidencePosition.FrontDoorPolicePed2:
                case ResidencePosition.FrontDoorPolicePed3:
                case ResidencePosition.SideWalkPolicePed1:
                case ResidencePosition.SideWalkPolicePed2:
                    return Color.DodgerBlue;
                case ResidencePosition.HidingSpot1:
                case ResidencePosition.HidingSpot2:
                    return Color.Orange;
                case ResidencePosition.PoliceParking1:
                case ResidencePosition.PoliceParking2:
                case ResidencePosition.PoliceParking3:
                case ResidencePosition.PoliceParking4:
                    return Color.Red;
                case ResidencePosition.FrontYardPedGroup:
                case ResidencePosition.SideYardPedGroup:
                case ResidencePosition.BackYardPedGroup:
                    return Color.White;
                case ResidencePosition.ResidentParking1:
                case ResidencePosition.ResidentParking2:
                default:
                    return Color.LightGreen;
            }
        }
    }
}
