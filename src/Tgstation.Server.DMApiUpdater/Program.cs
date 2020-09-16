using LibGit2Sharp;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Tgstation.Server.DMApiUpdater
{
	static class Program
	{
		static async Task<Tuple<byte[], string>> GetLatestDMApiBytes(IGitHubClient gitHubClient, string repoOwner, string repoName)
		{
			Console.WriteLine("Getting TGS releases...");
			var releases = await gitHubClient.Repository.Release.GetAll("tgstation", "tgstation-server");

			var latestDMApiRelease = releases.First(x => x.TagName.StartsWith("dmapi", StringComparison.Ordinal));
			Console.WriteLine($"Latest DMAPI release: {latestDMApiRelease.TagName}");

			var dmapiAsset = latestDMApiRelease.Assets.First(x => x.Name.Equals("DMAPI.zip", StringComparison.Ordinal));
			Console.WriteLine("Downloading zip...");

			using var webClient = new WebClient();
			return Tuple.Create(
				await webClient.DownloadDataTaskAsync(new Uri(dmapiAsset.BrowserDownloadUrl)),
				latestDMApiRelease.TagName);
		}

		static void RecursiveDelete(string directory)
		{
			foreach (var subDir in Directory.EnumerateDirectories(directory))
				RecursiveDelete(subDir);

			foreach (var file in Directory.EnumerateFiles(directory))
				File.Delete(file);

			Directory.Delete(directory);
		}

		static async Task Main(string[] args)
		{
			var headerPath = args[0];
			var libraryPath = args[1];
			var targetBranch = args[2];
			var gitHubToken = args[3];
			var repoSlug = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
			var repoSplits = repoSlug.Split('/');
			var repoOwner = repoSplits[0];
			var repoName = repoSplits[1];

			var gitHubClient = new GitHubClient(new ProductHeaderValue("Tgstation.Server.DMApiUpdater", "1.0.0"))
			{
				Credentials = new Octokit.Credentials(gitHubToken)
			};

			var releaseTask = GetLatestDMApiBytes(gitHubClient, repoOwner, repoName);

			var repoUrl = $"https://{gitHubToken}@github.com/{repoOwner}/{repoName}.git";
			Console.WriteLine($"Cloning {repoUrl}...");

			const string BaseRepoPath = "./cloned_repo";
			LibGit2Sharp.Repository.Clone(repoUrl, BaseRepoPath, new CloneOptions
			{
				BranchName = targetBranch,
				Checkout = true,
				RecurseSubmodules = false
			});

			Console.WriteLine("Loading repository...");
			using var repo = new LibGit2Sharp.Repository(BaseRepoPath);

			const string BranchName = "tgs-dmapi-updater-temp-branch";
			var workingBranch = repo.CreateBranch(BranchName);
			Commands.Checkout(repo, workingBranch);

			Console.WriteLine("Deleting old DMAPI...");

			var targetHeaderPath = Path.Combine(BaseRepoPath, headerPath);
			if (File.Exists(targetHeaderPath))
				File.Delete(targetHeaderPath);

			var targetLibraryPath = Path.Combine(BaseRepoPath, libraryPath);
			if (Directory.Exists(targetLibraryPath))
				RecursiveDelete(targetLibraryPath);

			var releaseTuple = await releaseTask;
			var zipBytes = releaseTuple.Item1;

			Console.WriteLine("Unzipping replacement DMAPI...");
			using var zipStream = new MemoryStream(zipBytes);
			using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
			foreach (var entry in archive.Entries)
			{
				if (entry.FullName.EndsWith('/'))
					continue;

				string targetPath;
				if (entry.Name.Equals("tgs.dm", StringComparison.Ordinal))
					targetPath = targetHeaderPath;
				else
					targetPath = Path.Combine(targetLibraryPath, "..", entry.FullName);

				var dir = Path.GetDirectoryName(targetPath);
				Directory.CreateDirectory(dir);
				entry.ExtractToFile(targetPath);
			}

			var status = repo.RetrieveStatus();
			if (!status.IsDirty)
			{
				Console.WriteLine("Repository is clean, nothing to do!");
				return;
			}

			Console.WriteLine("Staging files...");
			Commands.Stage(repo, "*");

			Console.WriteLine("Creating commit...");
			var signature = new LibGit2Sharp.Signature(
				new Identity(
					"tgstation-server",
					"tgstation-server@users.noreply.github.com"),
				DateTimeOffset.Now);
			repo.Commit($"Update to TGS {releaseTuple.Item2}", signature, signature);

			Console.WriteLine($"Force pushing to origin/{BranchName}...");
			var remote = repo.Network.Remotes.First();
			repo.Network.Push(remote, $"+refs/heads/{BranchName}");

			try
			{
				Console.WriteLine($"Creating pull request...");
				await gitHubClient.PullRequest.Create(
					repoOwner,
					repoName,
					new NewPullRequest("Update TGS DMAPI", BranchName, targetBranch)
					{
						Body = "This PR automatically updates the TGS DMAPI to the latest version. Please note that major changes need further modifications. Features added in minor updates may also require further implementation.",
						Draft = false,
						MaintainerCanModify = true
					});
			}
			catch (Exception ex) when (ex is ApiValidationException apiValidationException && apiValidationException.StatusCode == HttpStatusCode.UnprocessableEntity)
			{
				Console.WriteLine("Unable to create PR, it seems to already exist. It should be updated now.");
			}
		}
	}
}
