using LibGit2Sharp;

namespace WebsiteProxy
{
	public static class GitApi
	{
		static Identity identity = new Identity("fyrkantis", "david@kniberg.com");
		static PullOptions pullOptions;

		static GitApi()
		{
			MergeOptions mergeOptions = new MergeOptions();
			mergeOptions.FileConflictStrategy = CheckoutFileConflictStrategy.Theirs;
			pullOptions = new PullOptions();
			pullOptions.MergeOptions = mergeOptions;
		}

		public static void Pull(string path)
		{
			Repository repository = new Repository(path);
			MergeResult result = Commands.Pull(repository, new Signature(identity, DateTimeOffset.UtcNow), pullOptions);
		}
	}
}
