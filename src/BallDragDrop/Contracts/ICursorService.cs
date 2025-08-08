using System.Threading.Tasks;
using BallDragDrop.Models;

namespace BallDragDrop.Contracts
{
    /// <summary>
    /// Service for managing custom PNG cursors based on hand state
    /// </summary>
    public interface ICursorService
    {
        #region Methods

        /// <summary>
        /// Sets the cursor for the specified hand state
        /// </summary>
        /// <param name="handState">The hand state</param>
        void SetCursorForHandState(HandState handState);

        /// <summary>
        /// Reloads cursor configuration from settings
        /// </summary>
        Task ReloadConfigurationAsync();

        /// <summary>
        /// Gets the current cursor state for debugging
        /// </summary>
        /// <returns>Current cursor state description</returns>
        string GetCurrentCursorState();

        #endregion Methods
    }
}