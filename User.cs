using System.Web;

namespace WebsiteProxy
{
	public class User
	{
		public string id { get; set; }
		//private string _name;
		public string name { get; set; }
		/*{
			get { return _name; }
			set
			{
				_name = value;
				_dateUpdated = DateTime.UtcNow;
			}
		}*/
		public DateTime dateAdded { get; set; }
		//private DateTime _dateUpdated;
		public DateTime dateUpdated { get; set; }

		public User(string name) {
			id = Guid.NewGuid().ToString();
			this.name = name;
			dateAdded = DateTime.UtcNow;
			dateUpdated = dateAdded;
		}
	}
}
