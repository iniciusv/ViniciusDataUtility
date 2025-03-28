using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayGroundTransformer;
public class User
{
	public string GUID { get; set; }
	public DateTime Created { get; set; }
	public string ClientCode { get; set; }
	public string Description { get; set; }
	public int NCM { get; set; }
}
