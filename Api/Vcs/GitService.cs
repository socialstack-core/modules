using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Api.Configuration;
using Newtonsoft.Json.Linq;

namespace Api.Vcs
{
    /// <summary>
    /// Provides services for managing Git hooks and interacting with Git repository events.
    /// </summary>
    public partial class GitService : AutoService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GitService"/> class and installs Git hooks if needed.
        /// </summary>
        public GitService()
        {
            // InstallHooks();
            //
            // var gitConfig = AppSettings.GetSection("Git"); 
            //
            // if (gitConfig is null)
            // {
            //     return;
            // }
            //
            // GitHooks.PreCommit.AddEventListener(async evt =>
            // {
            //
            //     if (evt.StagedFiles.Count == 0)
            //     {
            //         Log.Error("GIT", "No files staged for commit. Please stage files before committing.");
            //         throw new InvalidOperationException("No files staged for commit. Please stage files before committing.");
            //     }
            //
            //     
            //     foreach (var file in evt.StagedFiles)
            //     {
            //
            //         if (
            //             file.StartsWith("Admin/Source/ThirdParty") ||
            //             file.StartsWith("UI/Source/ThirdParty") ||
            //             file.StartsWith("Email/Source/ThirdParty") ||
            //             file.StartsWith("Api/ThirdParty")
            //         )
            //         {
            //             // check appsettings.json to see if edits to the thirdparty directory are allowed
            //
            //             if (bool.TryParse(gitConfig["DisableThirdPartyEdits"], out bool disallowEdits) && disallowEdits)
            //             {
            //                 Log.Error("GIT", $"Commit blocked: {file} is in a third-party directory and edits are not allowed.");
            //                 throw new InvalidOperationException($"Commit blocked: {file} is in a third-party directory and edits are not allowed.");
            //             }
            //             else
            //             {
            //                 Log.Info("GIT", $"Commit allowed: {file} is in a third-party directory but edits are permitted by configuration.");
            //             }
            //         }
            //     }
            //     return evt;
            // });

        }

        /// <summary>
        /// Installs Git hook scripts into the <c>.git/hooks</c> directory if they are not already installed.
        /// </summary>
        private void InstallHooks()
        {
            if (IsGitInstalled() && IsNodeInstalled())
            {
                if (!Directory.Exists(".git/hooks"))
                {
                    Directory.CreateDirectory(".git/hooks");
                }

                Log.Info("GIT", "Installing git hooks");

                // string dir = "Api/ThirdParty/Vcs/hooks/bash/";

                try
                {
                    if (File.Exists(".git/hooks/commit-msg"))
                    {
                        File.Delete(".git/hooks/commit-msg");
                    }

                    if (File.Exists(".git/hooks/pre-commit"))
                    {
                        File.Delete(".git/hooks/pre-commit");
                    }

                    if (File.Exists(".git/hooks/pre-push"))
                    {
                        File.Delete(".git/hooks/pre-push");
                    }
                }
                catch (IOException ex)
                {
                    Log.Error("GIT", "Failed to delete existing commit-msg hook: " + ex.Message);
                }
                finally
                {
                    // File.Copy(dir + "/commit-msg", ".git/hooks/commit-msg");
                    // File.Copy(dir + "/pre-commit", ".git/hooks/pre-commit");
                    // File.Copy(dir + "/pre-push", ".git/hooks/pre-push");
                    
                    // Log.Info("GIT", "Installed git hooks");
                }
            }
        }

        /// <summary>
        /// Runs the pre-commit hook logic, collecting staged files and author information, 
        /// then dispatches a <see cref="PreCommitEvent"/>.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
        /// <exception cref="InvalidProgramException">Thrown if Git is not installed or available.</exception>
        public async ValueTask RunPreCommit()
        {
            if (!IsGitInstalled())
            {
                throw new InvalidProgramException("Git is not installed or not available in the system PATH.");
            }

            // Get staged files
            string staged = RunGitCommand("diff --cached --name-only");
            List<string> stagedFiles = staged.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries).ToList();

            // if (stagedFiles.Count == 0)
            // {
            //     throw new InvalidOperationException("No files staged for commit. Please stage files before committing.");
            // }

            // Get current user name from git config
            string author = RunGitCommand("config user.name");

            var evt = new PreCommitEvent
            {
                Author = author,
                StagedFiles = stagedFiles
            };

            await GitHooks.PreCommit.Dispatch(evt);
        }

        /// <summary>
        /// Runs the commit message hook logic, reading the commit message and author,
        /// then dispatches a <see cref="CommitMessageEvent"/>.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
        public async ValueTask RunCommitMessage()
        {
            // Read commit message from file
            string commitMessage = File.Exists(".git/COMMIT_EDITMSG")
                ? File.ReadAllText(".git/COMMIT_EDITMSG").Trim()
                : "[No commit message found]";

            // Get author from Git config
            string author = RunGitCommand("config user.name");

            var evt = new CommitMessageEvent
            {
                CommitMessage = commitMessage,
                Author = author
            };

            await GitHooks.CommitMessage.Dispatch(evt);
        }

        /// <summary>
        /// Runs the pre-push hook logic, collecting commits being pushed, remote URL, and author,
        /// then dispatches a <see cref="PrePushEvent"/>.
        /// </summary>
        /// <remarks>
        /// This method reads the refs being pushed from standard input, as provided by Git during the pre-push hook.
        /// </remarks>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
        public async ValueTask RunPrePush()
        {
            string remoteUrl = RunGitCommand("remote get-url origin");
            string author = RunGitCommand("config user.name");


            var evt = new PrePushEvent
            {
                Commits = [],
                RemoteRepository = remoteUrl,
                Author = author
            };

            await GitHooks.PrePush.Dispatch(evt);
        }

        /// <summary>
        /// Determines whether Git is installed and available in the system PATH.
        /// </summary>
        /// <returns><c>true</c> if Git is installed and the current directory is a Git repository; otherwise, <c>false</c>.</returns>
        public bool IsGitInstalled()
        {
            return IsToolAvailable("git", "--version") && Directory.Exists(".git");
        }

        /// <summary>
        /// Determines whether Node.js is installed and available in the system PATH.
        /// </summary>
        /// <returns><c>true</c> if Node.js is installed; otherwise, <c>false</c>.</returns>
        public bool IsNodeInstalled()
        {
            return IsToolAvailable("node", "--version");
        }

        /// <summary>
        /// Checks if the specified tool is available by running it with the provided arguments.
        /// </summary>
        /// <param name="command">The command or tool name to check.</param>
        /// <param name="args">Arguments to pass to the command.</param>
        /// <returns><c>true</c> if the command executes successfully (exit code 0); otherwise, <c>false</c>.</returns>
        private bool IsToolAvailable(string command, string args)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Runs a Git command and returns its trimmed standard output.
        /// </summary>
        /// <param name="arguments">The arguments to pass to the Git command.</param>
        /// <returns>The trimmed standard output of the Git command.</returns>
        private static string RunGitCommand(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return output;
        }
    }
}
