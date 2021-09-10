using AgencyDispatchFramework.Extensions;
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
    /// <summary>
    /// This partial class contains the logic and menu properties
    /// for the <see cref="Residence"/> location
    /// </summary>
    internal partial class DeveloperPluginMenu
    {
        #region Control Properties

        private UIMenuItem ResidenceCreateButton { get; set; }

        private UIMenuItem ResidenceEditButton { get; set; }

        private UIMenuItem ResidenceDeleteButton { get; set; }

        private UIMenuItem ResidenceLoadBlipsButton { get; set; }

        private UIMenuItem ResidenceClearBlipsButton { get; set; }

        private UIMenuItem ResidencePositionButton { get; set; }

        private UIMenuListItem ResidenceZoneButton { get; set; }

        private UIMenuListItem ResidenceClassButton { get; set; }

        private UIMenuItem ResidenceStreetButton { get; set; }

        private UIMenuItem ResidenceHintButton { get; set; }

        private UIMenuItem ResidenceNumberButton { get; set; }

        private UIMenuItem ResidenceUnitButton { get; set; }

        private UIMenuItem ResidenceSpawnPointsButton { get; set; }

        private UIMenuItem ResidenceFlagsButton { get; set; }

        private UIMenuItem ResidenceSaveButton { get; set; }

        #endregion Control Properties

        #region Properties

        private Dictionary<ResidenceFlags, UIMenuCheckboxItem> ResidenceFlagsItems { get; set; }

        private Dictionary<ResidencePosition, UIMenuItem<SpawnPoint>> ResidenceSpawnPointItems { get; set; }

        #endregion Control Properties

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
            ResidenceEditButton = new UIMenuItem("Edit Location", "Edit a Residence location that you are currently near. ~y~Blips must be loaded");
            ResidenceDeleteButton = new UIMenuItem("Delete Location", "Delete a Residence location that you are currently near. ~y~Blips must be loaded");
            ResidenceLoadBlipsButton = new UIMenuItem("Load Checkpoints", "Loads checkpoints in the world as well as blips on the map to show all saved ~y~Residence ~w~locations in this zone");
            ResidenceClearBlipsButton = new UIMenuItem("Clear Checkpoints", "Clears all checkpoints and blips loaded by the ~y~Load Checkpoints ~w~option");

            // Disable buttons by default
            ResidenceEditButton.Enabled = false;
            ResidenceDeleteButton.Enabled = false;
            ResidenceClearBlipsButton.Enabled = false;

            // Button Events
            ResidenceCreateButton.Activated += ResidenceCreateButton_Activated;
            ResidenceEditButton.Activated += ResidenceEditButton_Activated;
            ResidenceDeleteButton.Activated += ResidenceDeleteButton_Activated;
            ResidenceLoadBlipsButton.Activated += (s, e) =>
            {
                if (LoadZoneLocations(LocationsDB.Residences.Query(), Color.Red, LocationTypeCode.Residence))
                {
                    ResidenceLoadBlipsButton.Enabled = false;
                    ResidenceClearBlipsButton.Enabled = true;
                }
            };
            ResidenceClearBlipsButton.Activated += (s, e) =>
            {
                ResidenceLoadBlipsButton.Enabled = true;
                ResidenceClearBlipsButton.Enabled = false;
                ClearZoneLocations();
            };

            // Add buttons
            ResidenceUIMenu.AddItem(ResidenceCreateButton);
            ResidenceUIMenu.AddItem(ResidenceEditButton);
            ResidenceUIMenu.AddItem(ResidenceDeleteButton);
            ResidenceUIMenu.AddItem(ResidenceLoadBlipsButton);
            ResidenceUIMenu.AddItem(ResidenceClearBlipsButton);

            // Bind Buttons
            ResidenceUIMenu.BindMenuToItem(AddResidenceUIMenu, ResidenceCreateButton);
            ResidenceUIMenu.BindMenuToItem(AddResidenceUIMenu, ResidenceEditButton);

            // *************************************************
            // Add Residence UI Menu
            // *************************************************

            // Setup Buttons
            ResidencePositionButton = new UIMenuItem("Location", "Sets the street location coordinates for this home. Please stand on the street facing the home, and activate this button.");
            ResidenceNumberButton = new UIMenuItem("Building Number", "");
            ResidenceUnitButton = new UIMenuItem("Room/Unit Number", "");
            ResidenceStreetButton = new UIMenuItem("Street Name", "");
            ResidenceHintButton = new UIMenuItem("Location Hint", "");
            ResidenceClassButton = new UIMenuListItem("Class", "Sets the social class of this home");
            ResidenceZoneButton = new UIMenuListItem("Zone", "Selects the jurisdictional zone");
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
            AddResidenceUIMenu.AddItem(ResidenceHintButton);
            AddResidenceUIMenu.AddItem(ResidenceClassButton);
            AddResidenceUIMenu.AddItem(ResidenceZoneButton);
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
                var item = new UIMenuItem<SpawnPoint>(null, name, GetResidencePositionDesc(flag));
                item.Activated += ResidenceSpawnPointButton_Activated;
                ResidenceSpawnPointItems.Add(flag, item);

                // Add button
                ResidenceSpawnPointsUIMenu.AddItem(item);
            }

            // Add class flags
            foreach (SocialClass flag in Enum.GetValues(typeof(SocialClass)))
            {
                var name = Enum.GetName(typeof(SocialClass), flag);
                ResidenceClassButton.Collection.Add(flag, name);
            }

            // Register for events
            AddResidenceUIMenu.OnMenuChange += AddResidenceUIMenu_OnMenuChange;
            ResidenceFlagsUIMenu.OnMenuChange += ResidenceFlagsUIMenu_OnMenuChange;
        }

        #region Control Events

        /// <summary>
        /// Method called when the "Create New Residence" button is clicked.
        /// Clears all prior data.
        /// </summary>
        private void ResidenceCreateButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            //
            // Reset everything!
            //

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

            // Add Zones
            ResidenceZoneButton.Collection.Clear();
            ResidenceZoneButton.Collection.Add(GameWorld.GetZoneNameAtLocation(pos));

            // Reset buttons
            ResidenceNumberButton.Description = "";
            ResidenceNumberButton.RightBadge = UIMenuItem.BadgeStyle.None;

            ResidenceStreetButton.Description = "";
            ResidenceStreetButton.RightBadge = UIMenuItem.BadgeStyle.None;

            // Reset ticks
            ResidenceHintButton.Description = "";
            ResidenceHintButton.RightBadge = UIMenuItem.BadgeStyle.Tick; // Not required

            // Reset ticks
            ResidenceUnitButton.Description = "";
            ResidenceUnitButton.RightBadge = UIMenuItem.BadgeStyle.Tick; // Not required

            ResidenceFlagsButton.RightBadge = UIMenuItem.BadgeStyle.None;
            ResidenceSpawnPointsButton.RightBadge = UIMenuItem.BadgeStyle.None;
            ResidencePositionButton.RightBadge = UIMenuItem.BadgeStyle.None;

            // Update description
            AddResidenceUIMenu.SubtitleText = "Add New Residence";

            // Enable button
            ResidenceSaveButton.Enabled = true;
            Status = LocationUIStatus.Adding;
        }

        /// <summary>
        /// Method called when the Residence "Edit Location" button is clicked on the Residence UI menu
        /// </summary>
        private void ResidenceEditButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Grab item
            if (LocationCheckpoint?.Tag == null) return;

            // Ensure tag is set properly
            var editingItem = LocationCheckpoint.Tag as Residence;
            if (editingItem == null) return;

            // Reset flags
            foreach (var kvp in ResidenceFlagsItems)
            {
                kvp.Value.Checked = editingItem.Flags.Contains(kvp.Key);
            }

            // Are flags complete?
            if (ResidenceFlagsItems.Any(x => x.Value.Checked))
            {
                ResidenceFlagsButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
            }
            else
            {
                ResidenceFlagsButton.RightBadge = UIMenuItem.BadgeStyle.None;
            }

            // Reset spawn points
            bool complete = true;
            foreach (var item in ResidenceSpawnPointItems)
            {
                // Checking for incomplete spawn points
                if (editingItem.SpawnPoints.ContainsKey(item.Key))
                {
                    // Assign item as complete
                    var sp = editingItem.SpawnPoints[item.Key];
                    item.Value.Tag = sp;
                    item.Value.RightBadge = UIMenuItem.BadgeStyle.Tick;

                    // Create checkpoint!
                    var position = (int)item.Key;
                    var color = GetResidencePositionColor(item.Key);
                    var checkpoint = GameWorld.CreateCheckpoint(sp.Position, color, radius: 5f);
                    SpawnPointHandles.AddOrUpdate(position, checkpoint);
                }
                else
                {
                    item.Value.Tag = null;
                    item.Value.RightBadge = UIMenuItem.BadgeStyle.None;
                    complete = false;
                }
            }

            // Are spawnpoints complete?
            var spBadgeStyle = (!complete) ? UIMenuItem.BadgeStyle.None : UIMenuItem.BadgeStyle.Tick;
            ResidenceSpawnPointsButton.RightBadge = spBadgeStyle;

            // Grab player location
            var pos = Rage.Game.LocalPlayer.Character.Position;

            // Add Zones
            ResidenceZoneButton.Collection.Clear();
            ResidenceZoneButton.Collection.Add(GameWorld.GetZoneNameAtLocation(pos));

            // Reset buttons
            ResidenceNumberButton.Description = editingItem.BuildingNumber ?? "";
            ResidenceNumberButton.RightBadge = GetBadgeStyleByTextValue(editingItem.BuildingNumber);

            ResidenceStreetButton.Description = editingItem.StreetName ?? "";
            ResidenceStreetButton.RightBadge = GetBadgeStyleByTextValue(editingItem.StreetName);

            // Reset ticks
            ResidenceHintButton.Description = editingItem.Hint ?? "";
            ResidenceHintButton.RightBadge = UIMenuItem.BadgeStyle.Tick; // Not required

            // Reset ticks
            ResidenceUnitButton.Description = editingItem.UnitId ?? "";
            ResidenceUnitButton.RightBadge = UIMenuItem.BadgeStyle.Tick; // Not required
            ResidencePositionButton.RightBadge = UIMenuItem.BadgeStyle.Tick; // Already set

            // Update description
            AddResidenceUIMenu.SubtitleText = "Editing Residence";

            // Update position
            NewLocationPosition = new SpawnPoint(editingItem.Position, editingItem.Heading);

            // Enable button
            ResidenceSaveButton.Enabled = true;
            Status = LocationUIStatus.Editing;
        }

        /// <summary>
        /// Method called when the Residence "Delete Location" button is clicked on the Residence UI menu
        /// </summary>
        private void ResidenceDeleteButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Grab item
            if (LocationCheckpoint?.Tag == null) return;

            // Ensure tag is set properly
            var editingItem = LocationCheckpoint.Tag as Residence;
            if (editingItem == null) return;

            // Disable buttons
            DisableAllEditButtons();

            // Prevent app crashing
            try
            {
                // Delete location
                if (LocationsDB.Residences.Delete(editingItem.Id))
                {
                    // Delete checkpoint
                    DeleteBlipAndCheckpoint(LocationCheckpoint);
                    LocationCheckpoint = null;

                    // Notify the user
                    ShowNotification("Delete Residence", $"~g~Location deleted.");
                }
                else
                {
                    // Display notification to the player
                    ShowNotification("Delete Residence", $"~o~Unable to delete location from database.");
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);

                // Display notification to the player
                ShowNotification("Delete Residence", $"~r~Action Failed: ~o~Please check your Game.log!");
            }
        }

        /// <summary>
        /// Method called when the Residence "Set Position" button is clicked on the Residence UI menu
        /// </summary>
        private void ResidencePositionButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Delete old handle
            if (LocationCheckpoint != null)
            {
                LocationCheckpoint.Delete();
            }

            // Create checkpoint here
            var pos = GamePed.Player.Position;
            var cpPos = new Vector3(pos.X, pos.Y, pos.Z - ZCorrection);
            LocationCheckpoint = GameWorld.CreateCheckpoint(cpPos, Color.Purple);

            // Set new location
            NewLocationPosition = new SpawnPoint(pos, GamePed.Player.Heading);
            ResidencePositionButton.RightBadge = UIMenuItem.BadgeStyle.Tick;

            // Set street name default
            var streetName = GameWorld.GetStreetNameAtLocation(GamePed.Player.Position, out string crossRoad);
            if (!String.IsNullOrEmpty(streetName))
            {
                ResidenceStreetButton.Description = streetName;
                ResidenceStreetButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
            }

            // Add hint
            if (!String.IsNullOrEmpty(crossRoad))
            {
                ResidenceHintButton.Description = $"near {crossRoad}";
                ResidenceHintButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
            }
        }

        /// <summary>
        /// Method called when a residence "Spawnpoint" button is clicked on the Residence Spawn Points UI menu
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
                checkpoint.Delete();
            }

            // Create new checkpoint !!important, need to subtract 2 from the Z since checkpoints spawn at waist level
            var cpPos = new Vector3(pos.X, pos.Y, pos.Z - ZCorrection);
            checkpoint = GameWorld.CreateCheckpoint(cpPos, GetResidencePositionColor(value), radius: 1f);
            SpawnPointHandles.AddOrUpdate(index, checkpoint);

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
            var heading = (Status == LocationUIStatus.Adding) ? "Add" : "Edit";

            // Ensure everything is done
            var requiredItems = new[] { ResidenceFlagsButton, ResidenceNumberButton, ResidencePositionButton, ResidenceSpawnPointsButton, ResidenceStreetButton };
            foreach (var item in requiredItems)
            {
                if (item.RightBadge != UIMenuItem.BadgeStyle.Tick)
                {
                    // Display notification to the player
                    ShowNotification($"{heading} Residence", $"~o~Location does not have all required parameters set");
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
                ShowNotification($"{heading} Residence", $"~r~Save Failed: ~o~Unable to find zone in the locations database!");
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
                    Hint = ResidenceHintButton.Description,
                    UnitId = ResidenceUnitButton.Description,
                    Class = (SocialClass)ResidenceClassButton.SelectedValue,
                    Flags = ResidenceFlagsItems.Where(x => x.Value.Checked).Select(x => x.Key).ToList(),
                    SpawnPoints = ResidenceSpawnPointItems.ToDictionary(k => k.Key, v => v.Value.Tag),
                    Zone = zone
                };

                // Be nice and prevent locking up
                GameFiber.Yield();

                // Insert or update
                if (Status == LocationUIStatus.Editing)
                {
                    // Grab item
                    if (LocationCheckpoint?.Tag == null) return;

                    // Ensure tag is set properly
                    var editingItem = LocationCheckpoint.Tag as Residence;
                    if (editingItem == null) return;

                    // Set id for update
                    home.Id = editingItem.Id;

                    // Save location in the database
                    LocationsDB.Residences.Update(home);
                }
                else
                {
                    // Save location in the database
                    var id = LocationsDB.Residences.Insert(home);
                    home.Id = id.AsInt32;
                }

                // Editing
                LocationCheckpoint.Tag = home;

                // Display notification to the player
                ShowNotification($"~b~{heading} Residence.", $"~g~Location saved Successfully.");
            }
            catch (Exception e)
            {
                Log.Exception(e);

                // Display notification to the player
                ShowNotification($"~b~{heading} Residence.", $"~r~Save Failed: ~o~Please check your Game.log!");
            }


            // Go back
            AddResidenceUIMenu.GoBack();

            // Are we currently showing checkpoints and blips?
            if (Status == LocationUIStatus.Adding && ShowingZoneLocations)
            {
                var blip = new Blip(pos) { Color = Color.Yellow };
                LocationCheckpoint = GameWorld.CreateCheckpoint(pos, Color.Yellow, forceGround: true);
                ZoneCheckpoints.Add(LocationCheckpoint, blip);
            }
        }

        #endregion Control Events

        #region Menu Events

        private void AddResidenceUIMenu_OnMenuChange(UIMenu oldMenu, UIMenu newMenu, bool forward)
        {
            // Reset checkpoint handles
            if (!forward)
            {
                if (oldMenu == AddResidenceUIMenu)
                {
                    ResetCheckPoints();
                    Status = LocationUIStatus.None;
                }
            }
        }

        private void ResidenceFlagsUIMenu_OnMenuChange(UIMenu oldMenu, UIMenu newMenu, bool forward)
        {
            // Are we backing out of this menu?
            if (!forward && oldMenu == ResidenceFlagsUIMenu)
            {
                // We must have at least 1 item checked
                if (ResidenceFlagsItems.Any(x => x.Value.Checked))
                {
                    ResidenceFlagsButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
                }
                else
                {
                    ResidenceFlagsButton.RightBadge = UIMenuItem.BadgeStyle.None;
                }
            }
        }

        #endregion Menu Events

        /// <summary>
        /// Gets a color for a <see cref="Checkpoint"/> based on the <see cref="ResidencePosition"/>
        /// </summary>
        private Color GetResidencePositionColor(ResidencePosition value)
        {
            switch (value)
            {
                case ResidencePosition.BackDoorPed:
                case ResidencePosition.FrontDoorPed1:
                case ResidencePosition.FrontDoorPed2:
                case ResidencePosition.SidewalkPed:
                    return Color.Yellow;
                case ResidencePosition.BackDoorPolicePed:
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
                    return Color.Green;
                default:
                    return Color.HotPink;
            }
        }

        /// <summary>
        /// Gets the text description of a <see cref="ResidencePosition"/>
        /// </summary>
        private string GetResidencePositionDesc(ResidencePosition value)
        {
            switch (value)
            {
                case ResidencePosition.BackDoorPed: return "A ped standing at the ~g~Back Door or porch ~y~facing away from the door";
                case ResidencePosition.FrontDoorPed1: return "A ped standing at the ~g~Front Door ~y~facing away from the door";
                case ResidencePosition.FrontDoorPed2: return "A ped standing near the ~g~Front Door ~y~facing ~b~FrontDoorPolicePed2";
                case ResidencePosition.SidewalkPed: return "A ped standing on the ~g~Sidewalk in ~y~Front ~w~of the home";
                case ResidencePosition.BackDoorPolicePed: return "A police ped near the ~g~back door or porch ~y~Talking to ~b~BackDoorPed";
                case ResidencePosition.FrontDoorPolicePed1:
                case ResidencePosition.FrontDoorPolicePed2: return "A police ped near the ~g~Front Door or porch ~y~Talking to ~b~FrontDoorPed1";
                case ResidencePosition.FrontDoorPolicePed3: return "A police ped near the ~g~Front Door or porch ~y~Talking to or watching ~b~FrontDoorPed2";
                case ResidencePosition.SideWalkPolicePed1:
                case ResidencePosition.SideWalkPolicePed2: return "A police ped standing near the ~g~Sidewalk ~y~Talking to ~b~SidewalkPed";
                case ResidencePosition.HidingSpot1: 
                case ResidencePosition.HidingSpot2: return "A ~y~Hiding Spot ~w~for a ~o~Suspect ~w~to hide from the police";
                case ResidencePosition.PoliceParking1: 
                case ResidencePosition.PoliceParking2:
                case ResidencePosition.PoliceParking3:
                case ResidencePosition.PoliceParking4: return "A place for police car to park";
                case ResidencePosition.FrontYardPedGroup: return "An area for ~y~Peds ~w~to spawn in the ~g~Front Yard";
                case ResidencePosition.SideYardPedGroup: return "An area for ~y~Peds ~w~to spawn in a ~g~Side Yard";
                case ResidencePosition.BackYardPedGroup: return "An area for ~y~Peds ~w~to spawn in the ~g~Back Yard";
                case ResidencePosition.ResidentParking1:
                case ResidencePosition.ResidentParking2: return "A place for residents car to be parked";
                default:
                    return "Activate to set position. Character facing is important";
            }
        }
    }
}
