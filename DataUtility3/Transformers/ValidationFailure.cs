//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace DataUtility3.Transformers
//{
//	public class ValidationFailure
//	{
//		public ValidationFailure(string propertyName, string errorMessage)
//		{
//			PropertyName = propertyName;
//			ErrorMessage = errorMessage;
//		}

//		public string PropertyName { get; set; }
//		public string ErrorMessage { get; set; }

//		// Outras propriedades úteis que o FluentValidation inclui:
//		public object AttemptedValue { get; set; }
//		public object CustomState { get; set; }
//		public string ErrorCode { get; set; }
//		public string FormattedMessagePlaceholderValues { get; set; }

//		// Método para formatar a mensagem
//		public override string ToString() => ErrorMessage;
//	}
//}
