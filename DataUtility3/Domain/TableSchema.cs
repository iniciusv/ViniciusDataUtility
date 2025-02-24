using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataUtility.General;

public class TableSchema
{
	public List<string?>? DataTypes { get; set; }
	public List<string?>? Nullable { get; set; }
	public List<string?>? Special { get; set; }
}
