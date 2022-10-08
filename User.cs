using System.Web;

namespace WebsiteProxy
{
	public class User
	{
		public string name { get; set; }

		public User(string name) {
			this.name = name;
		}
	}
}
