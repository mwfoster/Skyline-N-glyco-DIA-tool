using System;
using System.Collections.Generic;

namespace SkylineNModFilter.Tests
{
    internal static class TestAssert
    {
        public static void True(bool value, string message)
        {
            if (!value)
                throw new Exception(message);
        }

        public static void False(bool value, string message)
        {
            True(!value, message);
        }

        public static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(message + " Expected: " + expected + "; Actual: " + actual);
        }

        public static TException Throws<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }

            throw new Exception(message + " Expected exception: " + typeof(TException).Name);
        }
    }
}
