using System;
using System.Threading;
using System.Threading.Tasks;
using BallDragDrop.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BallDragDrop.Tests
{
    [TestClass]
    public class EventThrottlingTests
    {
        /// <summary>
        /// Tests that the EventThrottler correctly throttles rapid events
        /// </summary>
        [TestMethod]
        public async Task TestEventThrottling()
        {
            // Counter for tracking how many times the action is executed
            int executionCount = 0;
            
            // Create an action that increments the counter
            Action action = () => Interlocked.Increment(ref executionCount);
            
            // Create an event throttler with a 100ms interval
            EventThrottler throttler = new EventThrottler(action, 100);
            
            // Execute the throttler 10 times in rapid succession
            for (int i = 0; i < 10; i++)
            {
                throttler.Execute();
                await Task.Delay(10); // Small delay between calls
            }
            
            // Wait for any pending executions to complete
            await Task.Delay(200);
            
            // The throttler should have executed the action only a few times, not 10 times
            // Typically it would execute once immediately and then once after the throttle period
            Assert.IsTrue(executionCount < 10, $"Expected fewer than 10 executions, but got {executionCount}");
            Assert.IsTrue(executionCount >= 1, "Expected at least one execution");
            
            // Reset the counter
            executionCount = 0;
            
            // Test ExecuteNow which should bypass throttling
            for (int i = 0; i < 5; i++)
            {
                throttler.ExecuteNow();
                await Task.Delay(10); // Small delay between calls
            }
            
            // ExecuteNow should execute every time
            Assert.AreEqual(5, executionCount, "ExecuteNow should execute every time without throttling");
        }
        
        /// <summary>
        /// Tests that the EventThrottler correctly spaces out executions
        /// </summary>
        [TestMethod]
        public async Task TestEventSpacing()
        {
            // Create a list to track execution times
            var executionTimes = new System.Collections.Generic.List<DateTime>();
            
            // Create an action that records the current time
            Action action = () => executionTimes.Add(DateTime.Now);
            
            // Create an event throttler with a 50ms interval
            EventThrottler throttler = new EventThrottler(action, 50);
            
            // Execute the throttler multiple times with delays that are less than the throttle interval
            for (int i = 0; i < 5; i++)
            {
                throttler.Execute();
                await Task.Delay(20); // 20ms is less than the 50ms throttle interval
            }
            
            // Wait for any pending executions to complete
            await Task.Delay(100);
            
            // Check that the executions were properly spaced
            Assert.IsTrue(executionTimes.Count >= 2, "Expected at least two executions");
            
            // Check the time between executions
            for (int i = 1; i < executionTimes.Count; i++)
            {
                TimeSpan timeBetweenExecutions = executionTimes[i] - executionTimes[i - 1];
                
                // Allow some tolerance for timing variations
                Assert.IsTrue(timeBetweenExecutions.TotalMilliseconds >= 40, 
                    $"Executions were too close together: {timeBetweenExecutions.TotalMilliseconds}ms");
            }
        }
        
        /// <summary>
        /// Tests that the EventThrottler correctly handles mixed Execute and ExecuteNow calls
        /// </summary>
        [TestMethod]
        public async Task TestMixedExecutionModes()
        {
            // Counter for tracking how many times the action is executed
            int executionCount = 0;
            
            // Create an action that increments the counter
            Action action = () => Interlocked.Increment(ref executionCount);
            
            // Create an event throttler with a 100ms interval
            EventThrottler throttler = new EventThrottler(action, 100);
            
            // Execute with mixed modes
            throttler.Execute();      // Should execute immediately
            await Task.Delay(10);
            throttler.Execute();      // Should be throttled
            await Task.Delay(10);
            throttler.ExecuteNow();   // Should execute immediately
            await Task.Delay(10);
            throttler.Execute();      // Should be throttled
            
            // Wait for any pending executions to complete
            await Task.Delay(200);
            
            // We expect 3 executions: 
            // 1. First Execute (immediate)
            // 2. ExecuteNow (bypasses throttling)
            // 3. Last Execute (after throttle period)
            Assert.AreEqual(3, executionCount, "Expected exactly 3 executions");
        }
    }
}