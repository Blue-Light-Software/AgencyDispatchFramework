using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Dispatching.Assignments;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Scripting;
using AgencyDispatchFramework.Simulation;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Drawing;
using System.Linq;
using System.Text;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// Plugin menu loaded when the player is OnDuty with ADF
    /// </summary>
    internal class DutyPluginMenu
    {
        /// <summary>
        /// Gets the banner menu name to display on all banners on all submenus
        /// </summary>
        private const string MENU_NAME = "ADF";

        private MenuPool AllMenus;

        private UIMenu MainUIMenu;
        private UIMenu DispatchUIMenu;
        private UIMenu PatrolUIMenu;
        private UIMenu WorldSettingsMenu;
        private UIMenu CallSignMenu;

        #region Main Menu Buttons

        private UIMenuItem DispatchMenuButton { get; set; }

        private UIMenuItem PatrolSettingsMenuButton { get; set; }

        private UIMenuItem ModSettingsMenuButton { get; set; }

        private UIMenuItem CloseMenuButton { get; set; }

        #endregion Main Menu Buttons

        #region Patrol Menu Buttons

        private UIMenuCheckboxItem SupervisorBox { get; set; }

        private UIMenuItem WorldSettingsButton { get; set; }

        private UIMenuItem CallSignsButton { get; set; }

        private UIMenuItem BeginSimuButton { get; set; }

        private UIMenuListItem ShiftSelectMenuItem { get; set; }

        private UIMenuListItem SetRoleMenuItem { get; set; }

        private UIMenuListItem PatrolAreaMenuButton { get; set; }

        private UIMenuListItem DivisionMenuButton { get; set; }

        private UIMenuListItem UnitTypeMenuButton { get; set; }

        private UIMenuListItem BeatMenuButton { get; set; }

        private UIMenuCheckboxItem RandomWeatherBox { get; set; }

        private UIMenuCheckboxItem RealisticWeatherBox { get; set; }

        private UIMenuCheckboxItem ForceWeatherBox { get; set; }

        private UIMenuItem SaveWorldSettingsButton { get; set; }

        private UIMenuListItem TimeScaleMenuItem { get; set; }

        private UIMenuCheckboxItem FastForwardBox { get; set; }

        private UIMenuCheckboxItem SyncTimeBox { get; set; }

        private UIMenuCheckboxItem SyncDateBox { get; set; }

        private UIMenuListItem WeatherMenuItem { get; set; }


        #endregion Patrol Menu Buttons

        #region Dispatch Menu Buttons

        private UIMenuCheckboxItem OutOfServiceButton { get; set; }

        private UIMenuListItem OfficerStatusMenuButton { get; set; }

        private UIMenuItem RequestQueueMenuButton { get; set; }

        private UIMenuItem RequestCallMenuButton { get; set; }

        private UIMenuItem EndCallMenuButton { get; set; }

        #endregion Dispatch Menu Buttons

        /// <summary>
        /// Indicates whether this menu is actively listening for key events
        /// </summary>
        internal bool IsListening { get; set; }

        /// <summary>
        /// Gets the <see cref="GameFiber"/> for this set of menus
        /// </summary>
        private GameFiber ListenFiber { get; set; }
       
        /// <summary>
        /// 
        /// </summary>
        public DutyPluginMenu()
        {
            // Create main menu
            MainUIMenu = new UIMenu(MENU_NAME, "~b~Main Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true
            };
            MainUIMenu.WidthOffset = 12;

            // Create main menu buttons
            DispatchMenuButton = new UIMenuItem("Dispatch Menu", "Opens the dispatch menu");
            PatrolSettingsMenuButton = new UIMenuItem("Patrol Settings", "Opens the patrol settings menu");
            CloseMenuButton = new UIMenuItem("Close", "Closes the main menu");

            // Add menu buttons
            MainUIMenu.AddItem(DispatchMenuButton);
            MainUIMenu.AddItem(PatrolSettingsMenuButton);
            MainUIMenu.AddItem(CloseMenuButton);

            // Register for button events
            CloseMenuButton.Activated += (s, e) => MainUIMenu.Visible = false;

            // Create Dispatch Menu
            BuildDispatchMenu();

            // Create Patrol Menu
            BuildPatrolMenu();

            // Bind Menus
            MainUIMenu.BindMenuToItem(DispatchUIMenu, DispatchMenuButton);
            MainUIMenu.BindMenuToItem(PatrolUIMenu, PatrolSettingsMenuButton);

            // Create menu pool
            AllMenus = new MenuPool
            {
                MainUIMenu,
                PatrolUIMenu,
                DispatchUIMenu,

                WorldSettingsMenu,
                CallSignMenu
            };

            // Refresh indexes
            AllMenus.RefreshIndex();
            MainUIMenu.OnMenuChange += MainUIMenu_OnMenuChange;
            PatrolUIMenu.OnMenuChange += PatrolUIMenu_OnMenuChange;
        }

        internal void BeginListening()
        {
            if (IsListening) return;
            IsListening = true;

            ListenFiber = GameFiber.StartNew(delegate
            {
                /*
                var openMenuKeyString = $"~{Settings.OpenCalloutMenuKey.GetInstructionalId()}~";
                var openMenuModifierKeyString = $"~{Settings.OpenCalloutMenuModifierKey.GetInstructionalId()}~";

                // Show Help
                Rage.Game.DisplayHelp($"Press the {openMenuModifierKeyString} ~+~ {openMenuKeyString} keys to open the interaction menu.", 6000);
                */

                // Open the menu initially when going on duty
                PatrolUIMenu.Visible = true;

                // Main loop
                while (IsListening)
                {
                    // Let other fibers do stuff
                    GameFiber.Yield();

                    // Process menus
                    AllMenus.ProcessMenus();

                    // If menu is closed, Wait for key press, then open menu
                    if (!AllMenus.IsAnyMenuOpen() && Keyboard.IsKeyDownWithModifier(Settings.OpenMenuKey, Settings.OpenMenuModifierKey))
                    {
                        if (!Simulation.Simulation.IsRunning)
                        {
                            PatrolUIMenu.Visible = true;
                        }
                        else
                        {
                            MainUIMenu.Visible = true;
                        }
                    }

                    // Enable/Disable buttons if not/on duty
                    if (MainUIMenu.Visible)
                    {
                        DispatchMenuButton.Enabled = Main.OnDutyLSPDFR;
                        //ModSettingsMenuButton.Enabled = Main.OnDuty;
                    }

                    // Disable patrol area selection if not highway patrol
                    if (DispatchUIMenu.Visible)
                    {
                        // Disable the Callout menu button if player is not on a callout
                        EndCallMenuButton.Enabled = Dispatch.ActivePlayerEvent != null;
                        RequestCallMenuButton.Enabled = Dispatch.CanInvokeAnyCalloutForPlayer(true);
                    }

                    // Enable / Disable menus
                    PatrolSettingsMenuButton.Enabled = !Simulation.Simulation.IsRunning;
                }
            });
        }

        internal void StopListening()
        {
            IsListening = false;
            ListenFiber = null;
        }

        private void BuildDispatchMenu()
        {
            // Create dispatch menu
            DispatchUIMenu = new UIMenu(MENU_NAME, "~b~Dispatch Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Create Dispatch Buttons
            OutOfServiceButton = new UIMenuCheckboxItem("Out Of Service", false);
            OutOfServiceButton.CheckboxEvent += OutOfServiceButton_CheckboxEvent;
            OfficerStatusMenuButton = new UIMenuListItem("Status", "Alerts dispatch to your current status. Click to set.");
            RequestCallMenuButton = new UIMenuItem("Request Call", "Requests a nearby call from dispatch");
            RequestQueueMenuButton = new UIMenuItem("Queue Crime Stats", "Requests current crime statistics from dispatch");
            EndCallMenuButton = new UIMenuItem("Code 4", "Tells dispatch the current call is complete.");

            // Fill List Items
            foreach (var role in Enum.GetValues(typeof(OfficerStatus)))
            {
                OfficerStatusMenuButton.Collection.Add(role, role.ToString());
            }

            // Create button events
            OfficerStatusMenuButton.Activated += (s, e) =>
            {
                var item = (OfficerStatus)OfficerStatusMenuButton.SelectedValue;
                Dispatch.SetPlayerStatus(item);

                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~b~Status Update",
                    "Status changed to: " + Enum.GetName(typeof(OfficerStatus), item)
                );
            };
            RequestCallMenuButton.Activated += RequestCallMenuButton_Activated;
            EndCallMenuButton.Activated += (s, e) => Dispatch.EndPlayerCallout();
            RequestQueueMenuButton.Activated += RequestQueueMenuButton_Activated;

            // Add dispatch menu buttons
            DispatchUIMenu.AddItem(OutOfServiceButton);
            DispatchUIMenu.AddItem(OfficerStatusMenuButton);
            DispatchUIMenu.AddItem(RequestQueueMenuButton);
            DispatchUIMenu.AddItem(RequestCallMenuButton);
            DispatchUIMenu.AddItem(EndCallMenuButton);
        }

        private void BuildPatrolMenu()
        {
            // Create patrol menu
            PatrolUIMenu = new UIMenu(MENU_NAME, "~b~Patrol Settings Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Create buttons
            SupervisorBox = new UIMenuCheckboxItem("Supervisor", false, "Enables supervisor mode.");
            WorldSettingsButton = new UIMenuItem("World Settings", "Setup the world settings for this shift.");
            CallSignsButton = new UIMenuItem("Choose CallSign", "Choose your CallSign for your Agency.");
            BeginSimuButton = new UIMenuItem("Begin Simulation", "Start the ADF simulation.") { BackColor = Color.Green, ForeColor = Color.Black };
            BeginSimuButton.Activated += BeginSimuButton_Activated;

            // Setup Shift items
            ShiftSelectMenuItem = new UIMenuListItem("Shift Selection", "Sets the shift you will be patrolling.");
            foreach (ShiftRotation shift in Enum.GetValues(typeof(ShiftRotation)))
            {
                ShiftSelectMenuItem.Collection.Add(shift, Enum.GetName(typeof(ShiftRotation), shift));
            }
            ShiftSelectMenuItem.OnListChanged += ShiftSelectMenuItem_OnListChanged;
            ShiftSelectMenuItem.Description = "Sets your desired shift hours. Day shift hours are 6am - 4pm";
            ShiftSelectMenuItem.Index = 0;

            // Setup Patrol Menu
            SetRoleMenuItem = new UIMenuListItem("Primary Role", "Sets your primary role in the department. This will determine that types of calls you will dispatched to.");
            foreach (UnitType role in Agency.GetCurrentPlayerAgency().GetSupportedUnitTypes())
            {
                SetRoleMenuItem.Collection.Add(role, Enum.GetName(typeof(UnitType), role));
            }

            // Build sub menus
            BuildWorldSettingsMenu();
            BuildCallsignsMenu();

            // Add patrol menu buttons
            PatrolUIMenu.AddItem(SupervisorBox);
            PatrolUIMenu.AddItem(SetRoleMenuItem);
            PatrolUIMenu.AddItem(ShiftSelectMenuItem);
            PatrolUIMenu.AddItem(WorldSettingsButton);
            PatrolUIMenu.AddItem(CallSignsButton);
            PatrolUIMenu.AddItem(BeginSimuButton);

            // Bind buttons
            PatrolUIMenu.BindMenuToItem(WorldSettingsMenu, WorldSettingsButton);
            PatrolUIMenu.BindMenuToItem(CallSignMenu, CallSignsButton);
        }

        private void BuildCallsignsMenu()
        {
            // Create patrol menu
            CallSignMenu = new UIMenu(MENU_NAME, "~b~CallSign Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            //
            if (Agency.GetCurrentPlayerAgency().CallSignStyle == CallSignStyle.LAPD)
            {
                // 
                DivisionMenuButton = new UIMenuListItem("Division", "Sets your division number.");
                for (int i = 1; i < 11; i++)
                {
                    string value = i.ToString();
                    DivisionMenuButton.Collection.Add(i, value);
                }

                // Find and set index
                var index = DivisionMenuButton.Collection.IndexOf(Settings.AudioDivision);
                if (index >= 0)
                {
                    DivisionMenuButton.Index = index;
                }

                BeatMenuButton = new UIMenuListItem("Beat", "Sets your Beat number.");
                for (int i = 1; i < 25; i++)
                {
                    string value = i.ToString();
                    BeatMenuButton.Collection.Add(i, value);
                }

                // Find and set index
                index = BeatMenuButton.Collection.IndexOf(Settings.AudioBeat);
                if (index >= 0)
                {
                    BeatMenuButton.Index = index;
                }
            }
            else
            {

            }
        }

        private void BuildWorldSettingsMenu()
        {
            // Create patrol menu
            WorldSettingsMenu = new UIMenu(MENU_NAME, "~b~World Settings Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Create buttons
            TimeScaleMenuItem = new UIMenuListItem("Timescale Multiplier", "Sets the timescale multipler. Default is 30 (1 second in real life equals 30 seconds in game)");
            FastForwardBox = new UIMenuCheckboxItem("Fast Forward to Shift", true, "If checked, when this menu closes, in game time will be fast forwarded to the begining of you shift");
            SyncTimeBox = new UIMenuCheckboxItem("Sync Time", false, "If checked, the clock in game will be sync'd with the current real time.") { Enabled = false };
            SyncDateBox = new UIMenuCheckboxItem("Sync Date", false, "If checked, the in game month, day and year will be set in game using todays date.");
            ForceWeatherBox = new UIMenuCheckboxItem("Force Weather", false, "If checked, the selected weather will be forced at when this menu closes.");
            RandomWeatherBox = new UIMenuCheckboxItem("Randomize Weather", false, "If checked, the weather will be selected at random.");
            RealisticWeatherBox = new UIMenuCheckboxItem("Realistic Weather", false, "If checked, the randomized weather will be sensible to the current month in game.") { Enabled = false };
            SaveWorldSettingsButton = new UIMenuItem("Save", "Saves the selected settings and goes back to the previous menu.");

            // Events
            FastForwardBox.CheckboxEvent += FastForwardBox_CheckboxEvent;
            ForceWeatherBox.CheckboxEvent += ForceWeatherBox_CheckboxEvent;
            RandomWeatherBox.CheckboxEvent += RandomWeatherBox_CheckboxEvent;
            SaveWorldSettingsButton.Activated += (s, e) => WorldSettingsMenu.GoBack();

            // Insert TimeScale valeus into the slider
            var currentMult = TimeScale.GetCurrentTimeScaleMultiplier();
            foreach (var number in Enumerable.Range(1, 30))
            {
                // Add item
                TimeScaleMenuItem.Collection.Add(number, number.ToString());

                // Set index to default
                if (currentMult == number)
                    TimeScaleMenuItem.Index = TimeScaleMenuItem.Collection.Count - 1;
            }

            // Weather selections
            WeatherMenuItem = new UIMenuListItem("Weather", "Sets the desired weather for the beggining of your shift") { Enabled = false };
            foreach (Weather weather in Enum.GetValues(typeof(Weather)))
            {
                WeatherMenuItem.Collection.Add(weather, Enum.GetName(typeof(Weather), weather));
            }

            // Add patrol menu buttons
            WorldSettingsMenu.AddItem(TimeScaleMenuItem);
            WorldSettingsMenu.AddItem(FastForwardBox);
            WorldSettingsMenu.AddItem(SyncTimeBox);
            WorldSettingsMenu.AddItem(SyncDateBox);
            WorldSettingsMenu.AddItem(RandomWeatherBox);
            WorldSettingsMenu.AddItem(RealisticWeatherBox);
            WorldSettingsMenu.AddItem(ForceWeatherBox);
            WorldSettingsMenu.AddItem(WeatherMenuItem);
            WorldSettingsMenu.AddItem(SaveWorldSettingsButton);
        }

        /// <summary>
        /// Disables and unchecks the Sync Clock button if the Fast Forward to Shift checkbox is checked
        /// or vise versa
        /// </summary>
        private void FastForwardBox_CheckboxEvent(UIMenuCheckboxItem sender, bool Checked)
        {
            if (Checked)
            {
                SyncTimeBox.Enabled = false;
                SyncTimeBox.Checked = false;
            }
            else
            {
                SyncTimeBox.Enabled = true;
            }
        }

        private void RandomWeatherBox_CheckboxEvent(UIMenuCheckboxItem sender, bool Checked)
        {
            if (Checked)
            {
                if (ForceWeatherBox.Enabled)
                {
                    ForceWeatherBox.Checked = false;
                    ForceWeatherBox.Enabled = false;
                }

                RealisticWeatherBox.Enabled = true;
            }
            else
            {
                if (!ForceWeatherBox.Enabled)
                {
                    ForceWeatherBox.Enabled = true;
                }
                
                RealisticWeatherBox.Enabled = false;
                RealisticWeatherBox.Checked = false;
            }
        }

        private void ForceWeatherBox_CheckboxEvent(UIMenuCheckboxItem sender, bool Checked)
        {
            if (Checked)
            {
                if (RandomWeatherBox.Enabled)
                {
                    RandomWeatherBox.Checked = false;
                    RandomWeatherBox.Enabled = false;
                }

                WeatherMenuItem.Enabled = true;
            }
            else
            {
                if (!RandomWeatherBox.Enabled)
                {
                    RandomWeatherBox.Enabled = true;
                }

                WeatherMenuItem.Enabled = false;
            }
        }

        private void ShiftSelectMenuItem_OnListChanged(UIMenuItem sender, int newIndex)
        {
            // Update description
            ShiftRotation item = (ShiftRotation)ShiftSelectMenuItem.SelectedValue;
            switch (item)
            {
                case ShiftRotation.Day:
                    ShiftSelectMenuItem.Description = "Sets your desired shift hours. Day shift hours are 6am - 4pm";
                    break;
                case ShiftRotation.Swing:
                    ShiftSelectMenuItem.Description = "Sets your desired shift hours. Swing shift hours are 3pm - 1am";
                    break;
                case ShiftRotation.Night:
                    ShiftSelectMenuItem.Description = "Sets your desired shift hours. Night shift hours are 9pm - 7am";
                    break;
            }
        }

        private void OutOfServiceButton_CheckboxEvent(UIMenuCheckboxItem sender, bool Checked)
        {
            var player = Dispatch.PlayerUnit;
            if (Checked)
            {
                player.Assignment = new OutOfService();

                // @todo change status to OutOfService
            }
            else
            {
                player.Assignment = null;
            }
        }

        private void MainUIMenu_OnMenuChange(UIMenu oldMenu, UIMenu newMenu, bool forward)
        {
            if (forward)
            {
                // Only set this when going forward
                if (newMenu == DispatchUIMenu)
                {
                    var status = Dispatch.GetPlayerStatus();
                    int index = OfficerStatusMenuButton.Collection.IndexOf(status);
                    OfficerStatusMenuButton.Index = index;
                }
            }
        }

        private void PatrolUIMenu_OnMenuChange(UIMenu oldMenu, UIMenu newMenu, bool forward)
        {
            if (forward)
            {

            }
            else
            {
                // If we are trying to exit the Patrol Settings menu into the main menu
                // without starting the simulation, close all menus
                if (oldMenu == PatrolUIMenu)
                {
                    if (!Simulation.Simulation.IsRunning)
                    {
                        AllMenus.CloseAllMenus();
                    }
                }
            }
        }

        private void RequestQueueMenuButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            var builder = new StringBuilder("Status: ");

            // Add status
            switch (Dispatch.CurrentCrimeLevel)
            {
                case CrimeLevel.VeryLow:
                    builder.Append("~g~It is currently very slow~w~");
                    break;
                case CrimeLevel.Low:
                    builder.Append("~g~It is slower than usual~w~");
                    break;
                case CrimeLevel.Moderate:
                    builder.Append("~b~Calls are coming in steady~w~");
                    break;
                case CrimeLevel.High:
                    builder.Append("~y~It is currently busy~w~");
                    break;
                case CrimeLevel.VeryHigh:
                    builder.Append("~o~We have lots of calls coming in~w~");
                    break;
            }

            // Add each call priority data
            foreach (EventPriority priority in Enum.GetValues(typeof(EventPriority)))
            {
                var calls = Dispatch.GetCallList(priority);
                int c1c = calls.Where(x => x.Status == EventStatus.Created || x.NeedsMoreOfficers).Count();
                int c1b = calls.Where(x => x.Status == EventStatus.Dispatched).Count() + c1c;
                builder.Append($"<br />- Priority {priority} Calls: ~b~{c1b} ~w~(~g~{c1c} ~w~Avail)");
            }

            // Display the information to the player
            Rage.Game.DisplayNotification(
                "3dtextures",
                "mpgroundlogo_cops",
                "Agency Dispatch Framework",
                "~b~Current Crime Statistics",
                builder.ToString()
            );
        }

        private void RequestCallMenuButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            RequestCallMenuButton.Enabled = false;
            if (!Dispatch.InvokeNextCalloutForPlayer(out bool dispatched))
            {
                Rage.Game.DisplayNotification("~r~You are currently not available for calls!");
            }
            else
            {
                if (!dispatched)
                {
                    Rage.Game.DisplayNotification("There are no calls currently available. ~g~Dispatch will send you the next call that comes in");
                }
            }
        }

        private void BeginSimuButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Disable button spam
            BeginSimuButton.Enabled = false;

            // Wrap to catch exceptions
            try
            {
                // Close menu
                AllMenus.CloseAllMenus();

                // @todo
                CallSign.TryParse("1L-18", out CallSign callSign);

                // Create the settings struct
                var settings = new SimulationSettings()
                {
                    PrimaryRole = (UnitType)SetRoleMenuItem.SelectedValue,
                    SelectedShift = (ShiftRotation)ShiftSelectMenuItem.SelectedValue,
                    TimeScaleMult = (int)TimeScaleMenuItem.SelectedValue,
                    FastForward = FastForwardBox.Checked,
                    SyncTime = SyncTimeBox.Checked,
                    SyncDate = SyncDateBox.Checked,
                    ForceWeather = ForceWeatherBox.Checked,
                    RandomWeather = RandomWeatherBox.Checked,
                    RealisticWeather = RealisticWeatherBox.Checked,
                    SelectedWeather = (Weather)WeatherMenuItem.SelectedValue,
                    SetCallSign = callSign
                };

                // Being simulation
                if (Simulation.Simulation.Begin(settings))
                {
                    // Display notification to the player
                    Rage.Game.DisplayNotification(
                        "3dtextures",
                        "mpgroundlogo_cops",
                        "Agency Dispatch Framework",
                        "~g~Plugin is Now Active.",
                        $"Now on duty serving ~g~{Dispatch.PlayerAgency.Zones.Length}~s~ zone(s)"
                    );
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);

                // Display notification to the player
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~o~Initialization Failed.",
                    $"~y~Please check your Game.log for errors."
                );
            }
        }
    }
}
