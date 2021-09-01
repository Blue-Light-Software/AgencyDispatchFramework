using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Dispatching.Assignments;
using AgencyDispatchFramework.Game;
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

        public UIMenuCheckboxItem SupervisorBox { get; private set; }
        public UIMenuCheckboxItem FastForwardBox { get; private set; }
        public UIMenuItem WorldSettingsButton { get; private set; }
        public UIMenuItem CallSignsButton { get; private set; }
        public UIMenuItem BeginSimuButton { get; private set; }
        public UIMenuListItem ShiftSelectMenuItem { get; private set; }

        #region Main Menu Buttons

        private UIMenuItem DispatchMenuButton { get; set; }

        private UIMenuItem PatrolSettingsMenuButton { get; set; }

        private UIMenuItem ModSettingsMenuButton { get; set; }

        private UIMenuItem CloseMenuButton { get; set; }

        #endregion Main Menu Buttons

        #region Patrol Menu Buttons

        private UIMenuListItem SetRoleMenuItem { get; set; }

        private UIMenuListItem PatrolAreaMenuButton { get; set; }

        private UIMenuListItem DivisionMenuButton { get; set; }

        private UIMenuListItem UnitTypeMenuButton { get; set; }

        private UIMenuListItem BeatMenuButton { get; set; }

        public UIMenuCheckboxItem RadomWeatherBox { get; private set; }

        public UIMenuItem SaveWorldSettingsButton { get; private set; }

        public UIMenuListItem TimeScaleMenuItem { get; private set; }

        public UIMenuListItem WeatherMenuItem { get; private set; }


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

                // Open the menu
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
                        MainUIMenu.Visible = true;
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
                        EndCallMenuButton.Enabled = Dispatch.PlayerActiveCall != null;
                        RequestCallMenuButton.Enabled = Dispatch.CanInvokeAnyCalloutForPlayer(true);
                    }
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
            FastForwardBox = new UIMenuCheckboxItem("Fast Forward to Shift", true, "If checked, when this menu closes time is fast forwarded to the begining of you shift");
            WorldSettingsButton = new UIMenuItem("World Settings", "Setup world settings.");
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

            // Build sub menus
            BuildWorldSettingsMenu();
            BuildCallsignsMenu();

            // Add patrol menu buttons
            PatrolUIMenu.AddItem(SupervisorBox);
            PatrolUIMenu.AddItem(SetRoleMenuItem);
            PatrolUIMenu.AddItem(ShiftSelectMenuItem);
            PatrolUIMenu.AddItem(FastForwardBox);
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
            RadomWeatherBox = new UIMenuCheckboxItem("Randomize Weather", false, "If checked, the weather will be selected at random.");
            SaveWorldSettingsButton = new UIMenuItem("Save", "Saves the selected settings and goes back to the previous menu.");

            // TimeScale slider
            TimeScaleMenuItem = new UIMenuListItem("Timescale Multiplier", "Sets the timescale multipler. Default is 30 (1 second in real life equals 30 seconds in game)");
            foreach (var number in Enumerable.Range(1, 30))
            {
                TimeScaleMenuItem.Collection.Add(number, number.ToString());
            }
            TimeScaleMenuItem.Index = 29; // Set to default

            // Weather selections
            WeatherMenuItem = new UIMenuListItem("Weather", "Sets the desired weather for the beggining of your shift");
            foreach (Weather weather in Enum.GetValues(typeof(Weather)))
            {
                WeatherMenuItem.Collection.Add(weather, Enum.GetName(typeof(Weather), weather));
            }

            // Add patrol menu buttons
            WorldSettingsMenu.AddItem(TimeScaleMenuItem);
            WorldSettingsMenu.AddItem(WeatherMenuItem);
            WorldSettingsMenu.AddItem(RadomWeatherBox);
            WorldSettingsMenu.AddItem(SaveWorldSettingsButton);
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
            if (!forward) return;

            if (newMenu == DispatchUIMenu)
            {
                var status = Dispatch.GetPlayerStatus();
                int index = OfficerStatusMenuButton.Collection.IndexOf(status);
                OfficerStatusMenuButton.Index = index;
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
            for (int i = 1; i < 5; i++)
            {
                var calls = Dispatch.GetCallList(i);
                int c1c = calls.Where(x => x.CallStatus == CallStatus.Created || x.NeedsMoreOfficers).Count();
                int c1b = calls.Where(x => x.CallStatus == CallStatus.Dispatched).Count() + c1c;
                builder.Append($"<br />- Priority {i} Calls: ~b~{c1b} ~w~(~g~{c1c} ~w~Avail)");
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
                if (Dispatch.BeginSimulation())
                {
                    // Yield to prevent freezing
                    GameFiber.Yield();

                    // Tell GameWorld to begin listening. Stops automatically when player goes off duty
                    GameWorld.BeginFibers();

                    // Display notification to the player
                    Rage.Game.DisplayNotification(
                        "3dtextures",
                        "mpgroundlogo_cops",
                        "Agency Dispatch Framework",
                        "~g~Plugin is Now Active.",
                        $"Now on duty serving ~g~{Dispatch.PlayerAgency.Zones.Length}~s~ zone(s)"
                    );
                }
                else
                {
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
