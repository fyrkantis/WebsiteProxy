using LibGit2Sharp;
using LibGit2Sharp.Handlers;

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
				Password = Util.environment["gitLoginToken"]
			};
			CredentialsHandler credentialsHandler = (_url, _user, _cred) => credentials;
			FetchOptions fetchOptions = new FetchOptions()
			{
				CredentialsProvider = credentialsHandler
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

		public static MergeResult? Pull(string path)
		{
			using(Repository repository = new Repository(path)) {
				return Commands.Pull(repository, new Signature(identity, DateTimeOffset.UtcNow), pullOptions);
			}
			return null;
		}
	}
}
