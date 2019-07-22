using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace FeedReaderTests
{
    public static class Util
    {
        
    }

    public static class AssertAsync
    {
        public static async Task ThrowsExceptionAsync<TException, TResult>(Func<Task<TResult>> action)
        {
            if (action == null)
                return;
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(TException))
                    return;
                throw new AssertFailedException($"Threw exception {ex.GetType().Name}, but exception {typeof(TException).Name} was expected.");
            }

            throw new AssertFailedException("Action did not throw an exception.");
        }

        public static async Task ThrowsExceptionAsync<TException>(Func<Task> action)
        {
            if (action == null)
                return;
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(TException))
                    return;
                throw new AssertFailedException($"Threw exception {ex.GetType().Name}, but exception {typeof(TException).Name} was expected.");
            }

            throw new AssertFailedException("Action did not throw an exception.");
        }
    }
}
