using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using LiteDB;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static Rage.Native.NativeFunction;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// Represents a basic <see cref="MenuPool"/> for the Agency Dispatch Framework Plugin
    /// </summary>
    /// <remarks>
    /// Plugin menu loaded when the player is OnDuty with LSPDFR, but not ADF
    /// </remarks>
    internal partial class DeveloperPluginMenu
    {
        /// <summary>
        /// Gets the banner menu name to display on all banners on all submenus
        /// </summary>
        private const string MENU_NAME = "ADF";

        /// <summary>
        /// When fetching the player position, the Z vector is set at the players waist level.
        /// To get the ground level <see cref="Vector3.Z"/>, subtract this value from the players
        /// <see cref="Vector3.Z"/> position.
        /// </summary>
        /// <remarks>Every charcter in GTA is 6' feet tall, or 1.8288m. This value is half of that.</remarks>
        public static readonly float ZCorrection = 0.9144f;

        #region Menus

        private MenuPool AllMenus;

        private UIMenu MainUIMenu;
        private UIMenu LocationsUIMenu;

        private UIMenu RoadUIMenu;
        private UIMenu AddRoadUIMenu;
        private UIMenu RoadSpawnPointsUIMenu;

        private UIMenu RoadShoulderFlagsUIMenu;
        private UIMenu RoadShoulderBeforeFlagsUIMenu;
        private UIMenu RoadShoulderAfterFlagsUIMenu;
        private UIMenu RoadShoulderSpawnPointsUIMenu;

        private UIMenu ResidenceUIMenu;
        private UIMenu AddResidenceUIMenu;
        private UIMenu ResidenceSpawnPointsUIMenu;
        private UIMenu ResidenceFlagsUIMenu;

        #endregion Menus

        #region Main Menu Buttons

        private UIMenuItem LocationsMenuButton { get; set; }

        private UIMenuListItem TeleportMenuButton { get; set; }

        private UIMenuItem CloseMenuButton { get; set; }

        #endregion Main Menu Buttons

        #region Locations Menu Buttons

        private UIMenuItem RoadShouldersButton { get; set; }

        private UIMenuItem ResidenceButton { get; set; }

        #endregion

        /// <summary>
        /// flagcode => handle
        /// </summary>
        private Dictionary<int, Checkpoint> SpawnPointHandles { get; set; }

        /// <summary>
        /// Gets a list of all currently active checkpoint handles in this <see cref="WorldZone"/>
        /// </summary>
        private Dictionary<Checkpoint, Blip> ZoneCheckpoints { get; set; }

        /// <summary>
        /// Gets or sets the current coordinates of the location we are editing
        /// </summary>
        private SpawnPoint NewLocationPosition { get; set; }

        /// <summary>
        /// Gets or sets the checkpoint handle that marks the current location being edited in game
        /// </summary>
        private Checkpoint LocationCheckpoint { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private LocationUIStatus Status { get; set; }

        /// <summary>
        /// Indicates wether zone checkpoints and blips are being displayed in game
        /// </summary>
        private bool ShowingZoneLocations { get; set; }

        /// <summary>
        /// The type of locaitons being shown via checkpoints and blips in game
        /// </summary>
        private LocationTypeCode LoadedBlipsLocationType { get; set; }

        /// <summary>
        /// Indicates to stop processing the controls of this menu while the keyboard is open
        /// </summary>
        internal bool IsKeyboardOpen { get; set; } = false;

        /// <summary>
        /// Indicates whether this menu is actively listening for key events
        /// </summary>
        internal bool IsListening { get; set; }

        /// <summary>
        /// Gets the <see cref="GameFiber"/> for this set of menus
        /// </summary>
        private GameFiber ListenFiber { get; set; }

        /// <summary>
        /// Creates a new isntance of <see cref="DeveloperPluginMenu"/>
        /// </summary>
        public DeveloperPluginMenu()
        {
            // Create main menu
            MainUIMenu = new UIMenu(MENU_NAME, "~b~Developer Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true
            };
            MainUIMenu.WidthOffset = 12;

            // Create menu buttons
            LocationsMenuButton = new UIMenuItem("Location Menu", "Allows you to view and add new locations for callouts");
            CloseMenuButton = new UIMenuItem("Close", "Closes this menu");

            // Cheater menu
            var places = new List<string>()
            {
                "Sandy", "Paleto", "Vespucci", "Rockford", "Downtown", "La Mesa", "Vinewood", "Davis"
            };
            TeleportMenuButton = new UIMenuListItem("Teleport", "Select police station to teleport to", places);
            
            // Add menu buttons
            MainUIMenu.AddItem(LocationsMenuButton);
            MainUIMenu.AddItem(TeleportMenuButton);
            MainUIMenu.AddItem(CloseMenuButton);

            // Register for button events
            LocationsMenuButton.Activated += LocationsMenuButton_Activated;
            TeleportMenuButton.Activated += TeleportMenuButton_Activated;
            CloseMenuButton.Activated += (s, e) => MainUIMenu.Visible = false;

            // Create RoadShoulders Menu
            BuildRoadShouldersMenu();

            // Create Residences Menu
            BuildResidencesMenu();

            // Create RoadShoulder Menu
            BuildLocationsMenu();

            // Bind Menus
            MainUIMenu.BindMenuToItem(LocationsUIMenu, LocationsMenuButton);

            // Create menu pool
            AllMenus = new MenuPool
            {
                MainUIMenu,
                LocationsUIMenu,
                AddRoadUIMenu,
                RoadUIMenu,
                RoadShoulderFlagsUIMenu,
                RoadShoulderBeforeFlagsUIMenu,
                RoadShoulderAfterFlagsUIMenu,
                RoadShoulderSpawnPointsUIMenu,
                AddResidenceUIMenu,
                ResidenceUIMenu,
                ResidenceFlagsUIMenu,
                ResidenceSpawnPointsUIMenu
            };

            // Refresh indexes
            AllMenus.RefreshIndex();

            // Create needed checkpoints
            SpawnPointHandles = new Dictionary<int, Checkpoint>(20);
            ZoneCheckpoints = new Dictionary<Checkpoint, Blip>(40);
        }

        private void BuildLocationsMenu()
        {
            // Create patrol menu
            LocationsUIMenu = new UIMenu(MENU_NAME, "~b~Location Editor")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Setup Buttons
            RoadShouldersButton = new UIMenuItem("Road Shoulders", "Manage road shoulder locations");
            ResidenceButton = new UIMenuItem("Residences", "Manage residence locations");

            // Add buttons
            LocationsUIMenu.AddItem(RoadShouldersButton);
            LocationsUIMenu.AddItem(ResidenceButton);

            // Bind buttons
            LocationsUIMenu.BindMenuToItem(RoadUIMenu, RoadShouldersButton);
            LocationsUIMenu.BindMenuToItem(ResidenceUIMenu, ResidenceButton);
        }

        #region Events

        /// <summary>
        /// Method called when a UIMenu item is clicked. The OnScreen keyboard is displayed,
        /// and the text that is typed will be saved in the description
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="selectedItem"></param>
        private void DispayKeyboard_SetDescription(UIMenu sender, UIMenuItem selectedItem)
        {
            GameFiber.StartNew(() =>
            {
                // Open keyboard
                Natives.DisplayOnscreenKeyboard(1, "FMMC_KEY_TIP8", "", selectedItem.Description, "", "", "", 48);
                IsKeyboardOpen = true;
                sender.InstructionalButtonsEnabled = false;
                Rage.Game.IsPaused = true;

                // Loop until the keyboard closes
                while (true)
                {
                    int status = Natives.UpdateOnscreenKeyboard<int>();
                    switch (status)
                    {
                        case 2: // Cancelled
                        case -1: // Not active
                            IsKeyboardOpen = false;
                            sender.InstructionalButtonsEnabled = true;
                            Rage.Game.IsPaused = false;
                            return;
                        case 0:
                            // Still editing
                            break;
                        case 1:
                            // Finsihed
                            string message = Natives.GetOnscreenKeyboardResult<string>();
                            selectedItem.Description = message;
                            selectedItem.RightBadge = UIMenuItem.BadgeStyle.Tick;
                            sender.InstructionalButtonsEnabled = true;
                            IsKeyboardOpen = false;
                            Rage.Game.IsPaused = false;
                            return;
                    }

                    GameFiber.Yield();
                }
            });
        }

        private void LocationsMenuButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Grab player location
            var pos = Rage.Game.LocalPlayer.Character.Position;
            LocationsUIMenu.SubtitleText = $"~y~{GameWorld.GetZoneNameAtLocation(pos)}~b~ Locations Menu";
        }

        private void TeleportMenuButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            Vector3 pos = Vector3.Zero;
            switch (TeleportMenuButton.SelectedValue)
            {
                case "Sandy":
                    pos = new Vector3(1848.73f, 3689.98f, 34.27f);
                    break;
                case "Paleto":
                    pos = new Vector3(-448.22f, 6008.23f, 31.72f);
                    break;
                case "Vespucci":
                    pos = new Vector3(-1108.18f, -845.18f, 19.32f);
                    break;
                case "Rockford":
                    pos = new Vector3(-561.65f, -131.65f, 38.21f);
                    break;
                case "Downtown":
                    pos = new Vector3(50.0654f, -993.0596f, 30f);
                    break;
                case "La Mesa":
                    pos = new Vector3(826.8f, -1290f, 28.24f);
                    break;
                case "Vinewood":
                    pos = new Vector3(638.5f, 1.75f, 82.8f);
                    break;
                case "Davis":
                    pos = new Vector3(360.97f, -1584.70f, 29.29f);
                    break;
            }

            // Just in case
            if (pos == Vector3.Zero) return;

            var player = Rage.Game.LocalPlayer;
            if (player.Character.IsInAnyVehicle(false))
            {
                // Find a safe vehicle location
                if (pos.GetClosestVehicleSpawnPoint(out SpawnPoint p))
                {
                    World.TeleportLocalPlayer(p, false);
                }
                else
                {
                    var location = World.GetNextPositionOnStreet(pos);
                    World.TeleportLocalPlayer(location, false);
                }
            }
            else
            {
                // Teleport player
                World.TeleportLocalPlayer(pos, false);
            }
        }

        #endregion Events

        /// <summary>
        /// Resets all check points just added by this position
        /// </summary>
        private void ResetCheckPoints()
        {
            // Delete all checkpoints
            foreach (var checkpoint in SpawnPointHandles.Values)
            {
                checkpoint.Delete();
            }

            // Clear checkpoint handles
            SpawnPointHandles.Clear();

            // Clear location check point
            if (LocationCheckpoint != null && !ShowingZoneLocations)
            {
                LocationCheckpoint.Delete();
            }

            // Set status to none
            Status = LocationUIStatus.None;
        }

        /// <summary>
        /// Deletes the checkpoints and blips loaded on the map
        /// </summary>
        private void ClearZoneLocations()
        {
            // Delete checkpoints
            foreach (var checkpoint in ZoneCheckpoints)
            {
                checkpoint.Key.Delete();
                checkpoint.Value.Delete();
            }

            // Clear collections
            ZoneCheckpoints.Clear();

            // Flag
            ShowingZoneLocations = false;
        }

        /// <summary>
        /// Loads checkpoints and map blips of all zone locations of the 
        /// specified type in the game and on the map
        /// </summary>
        /// <param name="queryable">The querable database instance to pull the locations from</param>
        /// <param name="color">The color to make the checkpoints in game</param>
        private bool LoadZoneLocations<T>(ILiteQueryable<T> queryable, Color color, LocationTypeCode typeCode) where T : WorldLocation
        {
            // Clear old shit
            ClearZoneLocations();
            LoadedBlipsLocationType = typeCode;

            // Get players current zone name
            var pos = GamePed.Player.Position;
            var zoneName = GameWorld.GetZoneNameAtLocation(pos);
            var enumName = typeof(T).Name;

            // Grab zone
            var zone = LocationsDB.WorldZones.FindOne(x => x.ScriptName.Equals(zoneName));
            if (zone == null)
            {
                // Display notification to the player
                ShowNotification("Load Zone Locations", $"~r~Failed: ~o~Unable to find WorldZone ~y~{zoneName} ~o~in the locations database!");
                return false;
            }

            // Now grab locations
            var items = queryable.Include(x => x.Zone).Where(x => x.Zone.Id == zone.Id).ToArray();
            if (items == null || items.Length == 0)
            {
                // Display notification to the player
                ShowNotification("Load Zone Locations", $"There are no {enumName} locations in the database");
                return false;
            }

            // Add each location as a checkpoint and blip
            foreach (T location in items)
            {
                // Create the checkpoint in the game world
                var vector = location.Position;
                var checkpoint = GameWorld.CreateCheckpoint(vector, color, forceGround: true);
                var blip = new Blip(vector) { Color = color };

                // Tag the location so we can edit it later
                checkpoint.Tag = location;

                // Add checkpoint and blip to a collection to keep tabs on it
                ZoneCheckpoints.Add(checkpoint, blip);
            }

            // Flag
            ShowingZoneLocations = true;
            return true;
        }

        /// <summary>
        /// Deletes the location checkpoint and blip
        /// </summary>
        /// <param name="locationCheckpoint"></param>
        private void DeleteBlipAndCheckpoint(Checkpoint locationCheckpoint)
        {
            // Delete checkpoint
            if (ZoneCheckpoints.TryGetValue(locationCheckpoint, out Blip blip))
            {
                // Remove checkpoint
                ZoneCheckpoints.Remove(locationCheckpoint);
                locationCheckpoint.Delete();

                // Delete paired blip
                blip.Delete();
            }
        }

        /// <summary>
        /// Short hand for showing a notification in game
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        private void ShowNotification(string title, string description)
        {
            // Display notification to the player
            Rage.Game.DisplayNotification(
                "3dtextures",
                "mpgroundlogo_cops",
                "Agency Dispatch Framework",
                title,
                description
            );
        }

        internal void BeginListening()
        {
            if (IsListening) return;
            IsListening = true;

            ListenFiber = GameFiber.StartNew(delegate 
            {
                while (IsListening)
                {
                    // Let other fibers do stuff
                    GameFiber.Yield();

                    // If keyboard is open, do not process controls!
                    if (IsKeyboardOpen) continue;

                    // Process menus
                    AllMenus.ProcessMenus();
                    var isAnyOpen = AllMenus.IsAnyMenuOpen();

                    // If menu is closed, Wait for key press, then open menu
                    if (!isAnyOpen && Keyboard.IsKeyDownWithModifier(Settings.OpenCalloutMenuKey, Settings.OpenCalloutMenuModifierKey))
                    {
                        MainUIMenu.Visible = true;
                        Status = LocationUIStatus.None;
                    }

                    // Are we in editing mode?
                    if (ShowingZoneLocations && ZoneCheckpoints.Count > 0)
                    {
                        if (isAnyOpen && Status == LocationUIStatus.None)
                        {
                            // Check if close to a checkpoint
                            var playerPos = GamePed.Player.Position;
                            var closestPoint = (
                                from x in ZoneCheckpoints
                                where x.Key.Position.DistanceTo(playerPos) < 7f
                                select x.Key).FirstOrDefault();

                            // Allow editing of the closest point
                            if (closestPoint != null)
                            {
                                // Did our closest location change?
                                if (!closestPoint.Equals(LocationCheckpoint))
                                {
                                    // Set
                                    LocationCheckpoint = closestPoint;

                                    // Reset buttons
                                    DisableAllEditButtons();
                                }

                                // Grab location from the checkpoint
                                var location = closestPoint.Tag as WorldLocation;
                                if (location == null) continue;

                                // Enable buttons
                                if (location.LocationType == LoadedBlipsLocationType)
                                {
                                    switch (LoadedBlipsLocationType)
                                    {
                                        case LocationTypeCode.RoadShoulder:
                                            RoadShoulderEditButton.Enabled = true;
                                            RoadShoulderDeleteButton.Enabled = true;
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                DisableAllEditButtons();
                            }
                        }
                    }
                    else
                    {
                        DisableAllEditButtons();
                    }
                }
            });
        }

        internal void DisableAllEditButtons()
        {
            RoadShoulderEditButton.Enabled = false;
            RoadShoulderDeleteButton.Enabled = false;
        }

        internal void StopListening()
        {
            IsListening = false;
            ListenFiber = null;
        }
    }
}
