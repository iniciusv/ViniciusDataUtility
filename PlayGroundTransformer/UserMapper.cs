using DataUtility3.Transformers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayGroundTransformer;

public class UserMapper : HeaderMapper<User>
{
	public UserMapper() : base("UserMapper")
	{
		Map("GUID", u => u.GUID);
		Map("Created", u => u.Created);
		Map("ClientCode", u => u.ClientCode);
		Map("Description", u => u.Description);
		Map("NCM", u => u.NCM); // Mapeia para int
	}
}