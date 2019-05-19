using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketsUWP
{
    // During handshake socket server has states ACK0, ACK2 etc.
    // Whilst socket client has states ACK1,ACK3 etc
    public enum SocketMode
    {
        ACK0,
        ACK1,
        ACK2,
        ACK3,
        ACK4,
        ACK5,
        ACK6,
        Disconnected,
        Starting,
        JustConnected,
        Connecting,
        Connected,
        Listening,
        Failed,
    }

    public enum ListeningMode { chr, ln, bffr,
        none
    }

    // In main app create a handler for status updates and set to <c>StatusUWPinstance.OnStatusUpdate</c>
    // StatusHandlerUWP class is instantaited in socket (client or server) class so need reference via that. eg:
    // socketSvr.StatusHandler.OnStatusMsgUpdate = OnStatusMsgUpdate;
    // socketSvr.StatusHandler.OnStatusChanged = OnStatusChanged;
    public delegate void UpdateStatusMsg(string status);
    public delegate void StatusChanged(SocketMode status);


    /// <summary>
    /// Class to handle the interchange beteen a socket class, <see cref="SocketClient"/> or <see cref="SocketServer"/>,
    /// and the main app wrt <see cref="SocketMode"/> changes and related messages.
    /// Is instantiated in both of the <see cref="SocketClient"/> and <see cref="SocketServer"/>.
    /// Note use of delegates for which methods are implemented and referenced in the main app.
    /// </summary>
    public class StatusHandlerUWP
    {
        /// <summary>
        /// In app handler for status changes
        /// </summary>
        public StatusChanged OnStatusChanged=null;

        /// <summary>
        /// In app handler fot status messages
        /// </summary>
        public UpdateStatusMsg OnStatusMsgUpdate = null;

        /// <summary>
        /// The last status message
        /// </summary>
        public string StatusMsg { get; set; } = "";


        private SocketMode currentMode = SocketMode.Disconnected;
        /// <summary>
        /// The current socket mode.
        /// </summary>
        public SocketMode CurrentMode { get => currentMode; set => currentMode = value; }

        /// <summary>
        /// Updates to status are done a separate Task so keep track to make sure
        /// they have all completed before closing app/this instance
        /// </summary>
        private List<Task> Tasks = null;




        /// <summary>
        /// Update status (as enum value) 
        /// and fire handler for its change if set.
        /// App handler will typically just display teh mode.
        /// </summary>
        /// <param name="mode">Mode Mode of enum type <see cref="SocketMode"/> to set as current for the socket</param>
        public void SetStatus(SocketMode mode)
        {
            if (currentMode != mode)
            {
                currentMode = mode;
                Task t = Task.Run(() =>
                {
                    OnStatusChanged?.Invoke(currentMode);
                });
                AddTask(t);
            }
        }

        /// <summary>
        /// Display a message inapp.
        /// Typically related to a mode change.
        /// But doesn't have to be.
        /// App handler will typically just display the message
        /// </summary>
        /// <param name="statusMsg">Message to display</param>
        public void SetStatusMsg(string statusMsg)
        {
            StatusMsg = statusMsg;
            Task t = Task.Run(() => {
                ;
                OnStatusMsgUpdate?.Invoke(StatusMsg);
            });
            AddTask(t);
        }

        /// <summary>
        /// Set mode and msg as one call.
        /// </summary>
        /// <param name="statusMsg">Message to display</param>
        /// <param name="mode">Mode of enum type <see cref="SocketMode"/> to set as current for the socket</param>
        public void Set(string statusMsg, SocketMode mode)
        {
            SetStatus(mode);
            SetStatusMsg(statusMsg);
        }

        /// <summary>
        /// Insert a message before the existing message.
        /// Requires app handler to have multiline control for the message.
        /// </summary>
        /// <param name="statusMsg">Message to prepend</param>
        public void PrependMsg(string statusMsg)
        {
            SetStatusMsg(statusMsg + "\r\n" + StatusMsg);
        }

        /// <summary>
        /// Append a message after the existing message.
        /// Requires app handler to have multiline control for the message.
        /// </summary>
        /// <param name="statusMsg">Message to append</param>
        public void AppendMsg(string statusMsg)
        {
            SetStatusMsg(StatusMsg + "\r\n" + statusMsg);
        }

        /// <summary>
        /// Clear the status message.
        /// Does not impact upon the current mode.
        /// </summary>
        public void Clear_StatusMsg()
        {
            SetStatusMsg("");
        }

        /// <summary>
        /// Status updates run as a separate task.
        /// Need to keep track of them.
        /// </summary>
        /// <param name="t">Task to add to Tasks list</param>
        public void AddTask(Task t)
        {
            Tasks.Add(t);
        }

        /// <summary>
        /// Wait for all tasks to complete when closing the socket 
        /// (and disposing of this class instance).
        /// </summary>
        public void AwaitTasks()
        {
            Task.WaitAll(Tasks.ToArray());
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        public StatusHandlerUWP()
        {
            Tasks = new List<Task>();
        }

        /// <summary>
        /// Class destructor
        /// </summary>
        ~StatusHandlerUWP()
        {
            AwaitTasks();
        }


    }
}
