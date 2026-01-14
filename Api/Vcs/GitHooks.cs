using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Vcs
{
    /// <summary>
    /// Contains Git hook event groups for different Git lifecycle events.
    /// </summary>
    public static partial class GitHooks
    {
        /// <summary>
        /// Event group for the Pre-Commit Git hook.
        /// Triggered before a commit is finalized.
        /// </summary>
        public static readonly GitHookEventGroup<PreCommitEvent> PreCommit = new();

        /// <summary>
        /// Event group for the Commit-Message Git hook.
        /// Triggered when editing or validating a commit message.
        /// </summary>
        public static readonly GitHookEventGroup<CommitMessageEvent> CommitMessage = new();

        /// <summary>
        /// Event group for the Pre-Push Git hook.
        /// Triggered before changes are pushed to a remote repository.
        /// </summary>
        public static readonly GitHookEventGroup<PrePushEvent> PrePush = new();
    }

    /// <summary>
    /// Represents a group of async event subscribers for a specific Git hook.
    /// Allows chaining of asynchronous processing on a given context.
    /// </summary>
    /// <typeparam name="T">The type of context passed through each subscriber.</typeparam>
    public class GitHookEventGroup<T>
    {
        private readonly List<Func<T, ValueTask<T>>> _subscribers = [];

        /// <summary>
        /// Adds a new asynchronous event listener to this hook group.
        /// </summary>
        /// <param name="subscriber">A function that takes a context and returns a modified context asynchronously.</param>
        public void AddEventListener(Func<T, ValueTask<T>> subscriber)
        {
            _subscribers.Add(subscriber);
        }

        /// <summary>
        /// Dispatches the context through all registered subscribers in sequence.
        /// Each subscriber may modify and return the context.
        /// </summary>
        /// <param name="context">The context object to be processed.</param>
        /// <returns>The final context after being processed by all subscribers.</returns>
        public async ValueTask<T> Dispatch(T context)
        {
            foreach (var sub in _subscribers)
            {
                context = await sub(context);
            }
            return context;
        }
    }
}
