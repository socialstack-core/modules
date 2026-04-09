using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Api.Eventing;

namespace Api.ESLint
{
    /// <summary>
    /// Handles ESLinting. 
    /// </summary>
    public partial class ESLintService : AutoService
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ESLintService()
        {
#if DEBUG
			var cfg = GetConfig<ESLintConfig>();
			
			if(cfg.Disabled){
				Log.Info(LogTag, "ESLint module disabled by configuration.");
				return;
			}
			
            Events.Compiler.BeforeCompile.AddEventListener((context, container) =>
            {
                TryRunESLint();
                return ValueTask.FromResult(container);
            });

            Events.FrontendChange.AddEventListener((context, container) =>
            {
                TryRunESLint();
                return ValueTask.FromResult(container);
            });
#endif
        }

        private object _locker = new();
        private bool esLintRunning = false;
        private bool runAgain = false;

        private void TryRunESLint()
        {
	        
	        lock (_locker)
	        {
		        if(esLintRunning){
			        runAgain = true;
			        return; // do nothing else
		        }
		        else
		        {
			        runAgain = false;
			        esLintRunning = true;
		        }
	        }

	        _ = RunESLint();
        }

        private async Task RunESLint()
        {
            Log.Info(LogTag, "Checking JS/TS for validity");
            string fileName;
            string arguments;

            // Choose correct shell command based on OS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName = "cmd.exe";
                arguments = "/c npx eslint --ext .ts,.tsx UI Email Admin -f json";
            }
            else
            {
                fileName = "/bin/bash";
                arguments = "-c \"npx eslint --ext .ts,.tsx UI Email Admin -f json\"";
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            string stdout = "";
            string stderr = "";

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    stdout += args.Data + "\n";
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    stderr += args.Data + "\n";
                }
            };

            try
            {
	            process.Start();
	            process.BeginOutputReadLine();
	            process.BeginErrorReadLine();

	            await process.WaitForExitAsync();

	            // If the process is successful, parse and log the output
	            if (process.ExitCode == 0)
	            {
		            Log.Info(LogTag, "[ESLint] All checks passed");
	            }
	            else if (process.ExitCode == 2)
	            {
		            Log.Error(LogTag, stderr);
	            }

	            // Parse and log the formatted output
	            PrintFormattedOutput(stdout);
            }
            catch (Exception ex)
            {
	            Log.Error(LogTag, $"[ESLint] failed to run: {ex.Message}");
            }
            finally
            {
	            var runAgainNow = false;
 
	            lock(_locker){
		            runAgainNow = runAgain;
		            esLintRunning = false;
		            runAgain = false; // not actually necessary but we tidy up anyway
	            }
 
	            if(runAgainNow){
		            // this thread is now permitted to spawn eslint (again)
		            await RunESLint();
	            }
            }
        }

        /// <summary>
        /// Parses and prints ESLint JSON output, associating errors with the correct file.
        /// </summary>
        private void PrintFormattedOutput(string rawOutput)
        {
            try
            {
                // Deserialize the ESLint JSON output
                var eslintResults = JsonSerializer.Deserialize<JsonArray>(rawOutput);

                if (eslintResults == null || eslintResults.Count == 0)
                {
                    Log.Info(LogTag, "No issues found.");
                    return;
                }

                // Process each file's linting results
                foreach (var fileResult in eslintResults)
                {
                    var fileObject = fileResult.AsObject();

                    var filePath = fileObject["filePath"]?.GetValue<string>();
                    var messages = fileObject["messages"]?.AsArray();

                    if (string.IsNullOrEmpty(filePath) || messages == null || messages.Count == 0)
                    {
                        continue;
                    }

                    filePath = filePath.Replace(Environment.CurrentDirectory + '\\', "");

                    // Log the issues for this file
                    foreach (var message in messages)
                    {
                        var severity = message["severity"]?.GetValue<int>() == 2 ? "ERROR" : "WARNING";
                        var lineNumber = message["line"]?.GetValue<int>();
                        var column = message["column"]?.GetValue<int>();
                        var messageText = message["message"]?.GetValue<string>();
                        var ruleId = message["ruleId"]?.GetValue<string>();

                        if (lineNumber.HasValue && column.HasValue && !string.IsNullOrEmpty(messageText))
                        {
                            var formatted = $"{filePath}:{lineNumber}:{column} ({ruleId}) - {messageText}";

                            if (severity == "ERROR")
                            {
                                Log.Error(LogTag, formatted);
                            }
                            else
                            {
                                Log.Warn(LogTag, formatted);
                            }
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Log.Error(LogTag, $"Error parsing ESLint output: {ex.Message}");
            }
        }
    }
}
