using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Modules.VB6.Collectors
{
    class CollectorVisitor : VisualBasic6BaseListener
    {
        public static SyntaxTreeHelper Helper = new SyntaxTreeHelper();

        public override void EnterType(VisualBasic6Parser.TypeContext context)
        {
            // As String, Worksheet, etc - including non-VB types like Worksheet etc.
            string customVariableType = Helper.GetCustomVariableType(context);
            if (customVariableType.Length > 0 && (DataCollector.AllTypes.Contains(customVariableType) || DataCollector.AllEnums.Contains(customVariableType)))
            {
                DataCollector.AllIdentifiers.Add(customVariableType);
            }
            base.EnterType(context);
        }

        public override void EnterTypeStmt(VisualBasic6Parser.TypeStmtContext context)
        {
            DataCollector.AllIdentifiers.Add(Helper.GetStatementIdentifier(context));
            base.EnterTypeStmt(context);
        }

        public override void EnterEnumerationStmt(VisualBasic6Parser.EnumerationStmtContext context)
        {
            DataCollector.AllIdentifiers.Add(Helper.GetStatementIdentifier(context));
            base.EnterEnumerationStmt(context);
        }

        public override void EnterEnumerationStmt_Constant(VisualBasic6Parser.EnumerationStmt_ConstantContext context)
        {
            DataCollector.AllIdentifiers.Add(Helper.GetStatementIdentifier(context));
            base.EnterEnumerationStmt_Constant(context);
        }

        public override void EnterTypeStmt_Element(VisualBasic6Parser.TypeStmt_ElementContext context)
        {
            DataCollector.AllIdentifiers.Add(Helper.GetStatementIdentifier(context));
            base.EnterTypeStmt_Element(context);
        }

        public override void EnterVariableSubStmt(VisualBasic6Parser.VariableSubStmtContext context)
        {
            DataCollector.AllIdentifiers.Add(Helper.GetStatementIdentifier(context));
            base.EnterVariableSubStmt(context);
        }

        public override void EnterConstSubStmt(VisualBasic6Parser.ConstSubStmtContext context)
        {
            DataCollector.AllIdentifiers.Add(Helper.GetStatementIdentifier(context));
            base.EnterConstSubStmt(context);
        }

        public override void EnterFunctionStmt(VisualBasic6Parser.FunctionStmtContext context)
        {
            if (!Helper.IsReserved(Helper.GetStatementIdentifier(context)))
            {
                DataCollector.AllIdentifiers.Add(Helper.GetStatementIdentifier(context));
            }
            base.EnterFunctionStmt(context);
        }

        public override void EnterSubStmt(VisualBasic6Parser.SubStmtContext context)
        {
            if (!Helper.IsReserved(Helper.GetStatementIdentifier(context)))
            {
                DataCollector.AllIdentifiers.Add(Helper.GetStatementIdentifier(context));
            }
            base.EnterSubStmt(context);
        }

        public override void EnterDeclareStmt([NotNull] VisualBasic6Parser.DeclareStmtContext context)
        {
            DataCollector.AllIdentifiers.Add(Helper.GetStatementIdentifier(context));
            base.EnterDeclareStmt(context);
        }

        public override void EnterArg([NotNull] VisualBasic6Parser.ArgContext context)
        {
            DataCollector.AllIdentifiers.Add(Helper.GetStatementIdentifier(context));
            base.EnterArg(context);
        }
    }
}
