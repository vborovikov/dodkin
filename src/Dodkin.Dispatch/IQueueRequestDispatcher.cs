namespace Dodkin.Dispatch;

using System;
using System.Threading.Tasks;
using Relay.RequestModel;

/// <summary>
/// Represents a queue operator that dispatches requests to the appropriate handlers.
/// </summary>
public interface IQueueRequestDispatcher : IRequestDispatcher
{
    /// <summary>
    /// Executes a command asynchronously with a timeout.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="timeout">The timeout.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ExecuteAsync<TCommand>(TCommand command, TimeSpan timeout) where TCommand : ICommand;

    /// <summary>
    /// Executes a query asynchronously and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the query.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="timeout">The timeout.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
    Task<TResult> RunAsync<TResult>(IQuery<TResult> query, TimeSpan timeout);
}

/// <summary>
/// Represents a queue operator that schedules commands to the appropriate handlers.
/// </summary>
public interface IQueueRequestScheduler : IQueueRequestDispatcher, IRequestScheduler
{
    /// <summary>
    /// Schedules a command to be executed at a specific time.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="at">The time at which the command should be executed.</param>
    /// <param name="timeout">The timeout.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ScheduleAsync<TCommand>(TCommand command, DateTimeOffset at, TimeSpan timeout) where TCommand : ICommand;
}
