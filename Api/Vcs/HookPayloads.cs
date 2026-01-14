using System.Collections.Generic;

namespace Api.Vcs
{
    /// <summary>
    /// Represents data for an event that occurs just before a commit is created in the version control system.
    /// </summary>
    public struct PreCommitEvent
    {
        /// <summary>
        /// Gets or sets the author of the commit.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the list of files staged for commit.
        /// </summary>
        public List<string> StagedFiles { get; set; }
    }

    /// <summary>
    /// Represents data for an event triggered when a commit message is being edited.
    /// </summary>
    public struct CommitMessageEvent
    {
        /// <summary>
        /// Gets or sets the commit message being edited.
        /// </summary>
        public string CommitMessage { get; set; }

        /// <summary>
        /// Gets or sets the author of the commit.
        /// </summary>
        public string Author { get; set; }
    }

    /// <summary>
    /// Represents data for an event that occurs just before changes are pushed to a remote repository.
    /// </summary>
    public struct PrePushEvent
    {
        /// <summary>
        /// Gets or sets the list of commit hashes being pushed.
        /// </summary>
        public List<string> Commits { get; set; }

        /// <summary>
        /// Gets or sets the remote repository that the commits are being pushed to.
        /// </summary>
        public string RemoteRepository { get; set; }

        /// <summary>
        /// Gets or sets the author of the commits being pushed.
        /// </summary>
        public string Author { get; set; }
    }
}
