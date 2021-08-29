using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Dispatching.Assignments;
using AgencyDispatchFramework.Simulation;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Linq;
using System.Text;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// Plugin menu loaded when the player is OnDuty with ADF
    /// </summary>
    internal class OnDutyPluginMenu
    {
        /// <summary>
        /// Gets the banner menu name to display on all banners on all submenus
        /// </summary>
        private const string MENU_NAME = "ADF";

        private MenuPool AllMenus;

        private UIMenu MainUIMenu;
        private UIMenu DispatchUIMenu;

        #region Main Menu Buttons

        private UIMenuItem DispatchMenuButton { get; set; }

        private UIMenuItem ModSettingsMenuButton { get; set; }

        private UIMenuItem CloseMenuButton { get; set; }

        #endregion Main Menu Buttons

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
        public OnDutyPluginMenu()
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
            CloseMenuButton = new UIMenuItem("Close", "Closes the main menu");

            // Add menu buttons
            MainUIMenu.AddItem(DispatchMenuButton);
            MainUIMenu.AddItem(CloseMenuButton);

            // Register for button events
            CloseMenuButton.Activated += (s, e) => MainUIMenu.Visible = false;

            // Create Dispatch Menu
            BuildDispatchMenu();

            // Bind Menus
            MainUIMenu.BindMenuToItem(DispatchUIMenu, DispatchMenuButton);

            // Create menu pool
            AllMenus = new MenuPool
            {
                MainUIMenu,
                DispatchUIMenu,
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
                        DispatchMenuButton.Enabled = Main.OnDuty;
                        ModSettingsMenuButton.Enabled = Main.OnDuty;
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
    }
}
