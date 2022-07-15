using LibGit2Sharp;

namespace WebsiteProxy
{
	public static class GitApi
	{
		static Identity identity = new Identity("fyrkantis", "david@kniberg.com");
		static PullOptions pullOptions;

		static GitApi()
		{
			Credentials credentials = new UsernamePasswordCredentials()
			{
				Username = Util.environment["gitUsername"],
				Password = Util.environment["gitPassword"]
			};
			FetchOptions fetchOptions = new FetchOptions()
			{
				CredentialsProvider = (_url, _user, _cred) => credentials
			};
			MergeOptions mergeOptions = new MergeOptions()
			{
				FileConflictStrategy = CheckoutFileConflictStrategy.Theirs
			};
			pullOptions = new PullOptions()
			{
				MergeOptions = mergeOptions,
				FetchOptions = fetchOptions
			};
		}

		public static void Pull(string path)
		{
			Repository repository = new Repository(path);
			MergeResult result = Commands.Pull(repository, new Signature(identity, DateTimeOffset.UtcNow), pullOptions);
		}
	}
}
