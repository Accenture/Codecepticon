using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.VB6.Collectors
{
    class TypeCollectorVisitor : VisualBasic6BaseListener
    {
        public static SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public override void EnterTypeStmt(VisualBasic6Parser.TypeStmtContext context)
        {
            DataCollector.AllTypes.Add(Helper.GetStatementIdentifier(context));
            base.EnterTypeStmt(context);
        }

        public override void EnterEnumerationStmt(VisualBasic6Parser.EnumerationStmtContext context)
        {
            DataCollector.AllEnums.Add(Helper.GetStatementIdentifier(context));
            base.EnterEnumerationStmt(context);
        }
    }
}
