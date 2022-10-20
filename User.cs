using System.Web;

namespace WebsiteProxy
{
	public class User
	{
		public string id { get; set; }
		public string name { get; set; }

		public User(string name) {
			id = Guid.NewGuid().ToString();
			this.name = name;
		}
	}
}
