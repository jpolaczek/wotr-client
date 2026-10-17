using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Wotr.Client.Tests
{
    // Assert.ThrowsAsync waits synchronously: in the Editor the awaited download resumes on the main thread it
    // blocks, and the test never ends. This awaits instead; a derived exception (TaskCanceledException) counts.
    internal static class Expect
    {
        public static async Task<T> Throws<T>(Func<Task> action) where T : Exception
        {
            try
            {
                await action();
            }
            catch (T error)
            {
                return error;
            }
            Assert.Fail($"Expected {typeof(T).Name}.");
            return null;
        }
    }
}
