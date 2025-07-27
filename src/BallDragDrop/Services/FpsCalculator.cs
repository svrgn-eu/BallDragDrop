using System;
using System.Collections.Generic;
using System.Linq;
using BallDragDrop.Models;

namespace BallDragDrop.Services
{
    /// <summary>
    /// Utility class for calculating 10-second rolling average FPS
    /// </summary>
    public class FpsCalculator
    {
        #region Fields

        /// <summary>
        /// List storing FPS readings for the last 10 seconds
        /// </summary>
        private readonly List<FpsReading> _fpsReadings;

        /// <summary>
        /// Lock object for thread-safe access to FPS readings
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Time window for calculating average (10 seconds)
        /// </summary>
        private readonly TimeSpan _timeWindow = TimeSpan.FromSeconds(10);

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets the current 10-second average FPS
        /// </summary>
        public double AverageFps
        {
            get
            {
                lock (_lock)
                {
                    return GetAverageFps();
                }
            }
        }

        #endregion Properties

        #region Construction

        /// <summary>
        /// Initializes a new instance of the FpsCalculator class
        /// </summary>
        public FpsCalculator()
        {
            _fpsReadings = new List<FpsReading>();
        }

        #endregion Construction

        #region Methods

        /// <summary>
        /// Adds a new FPS reading to the calculation
        /// </summary>
        /// <param name="fps">The FPS value to add</param>
        public void AddFpsReading(double fps)
        {
            // Validate FPS value - filter out invalid values
            if (fps < 0 || fps > 1000 || double.IsNaN(fps) || double.IsInfinity(fps))
            {
                return; // Skip invalid FPS values
            }

            lock (_lock)
            {
                var timestamp = DateTime.Now;
                _fpsReadings.Add(new FpsReading(fps, timestamp));

                // Remove readings older than 10 seconds
                CleanupOldReadings(timestamp);
            }
        }

        /// <summary>
        /// Gets the current 10-second average FPS
        /// </summary>
        /// <returns>The average FPS over the last 10 seconds, or 0 if no valid readings</returns>
        public double GetAverageFps()
        {
            lock (_lock)
            {
                try
                {
                    var currentTime = DateTime.Now;
                    CleanupOldReadings(currentTime);

                    if (_fpsReadings.Count == 0)
                    {
                        return 0.0;
                    }

                    // Calculate average from valid readings
                    var average = _fpsReadings.Average(r => r.Fps);
                    
                    // Ensure the result is valid
                    if (double.IsNaN(average) || double.IsInfinity(average))
                    {
                        return 0.0;
                    }
                    
                    return average;
                }
                catch (Exception)
                {
                    // Return 0 on any calculation error
                    return 0.0;
                }
            }
        }

        /// <summary>
        /// Removes FPS readings older than the time window
        /// </summary>
        /// <param name="currentTime">Current timestamp for comparison</param>
        private void CleanupOldReadings(DateTime currentTime)
        {
            var cutoffTime = currentTime - _timeWindow;
            _fpsReadings.RemoveAll(r => r.Timestamp < cutoffTime);
        }

        /// <summary>
        /// Gets the number of FPS readings currently stored
        /// </summary>
        /// <returns>The count of FPS readings</returns>
        public int GetReadingCount()
        {
            lock (_lock)
            {
                return _fpsReadings.Count;
            }
        }

        /// <summary>
        /// Clears all FPS readings
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _fpsReadings.Clear();
            }
        }

        #endregion Methods
    }
}