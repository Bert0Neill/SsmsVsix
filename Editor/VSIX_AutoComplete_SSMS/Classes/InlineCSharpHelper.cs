using Microsoft.CSharp;
using Syncfusion.Windows.Forms;
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using VSIX_AutoComplete_SSMS.Models;
using VSIX_Demo;
using SQL_Shortcuts.Resources;

namespace VSIX_AutoComplete_SSMS.Classes
{
    public class InlineCSharpHelper
    {
        public InlineCSharpHelper() { }

        public string EvaluateInlineCSharp(string input)
        {
            try
            {
                const string pattern = @"\{(.*?)\}";
                var regex = new Regex(pattern);
                var matches = regex.Matches(input);

                var result = new StringBuilder();
                int lastIndex = 0;

                foreach (Match match in matches)
                {
                    result.Append(input.Substring(lastIndex, match.Index - lastIndex));
                    string expression = match.Groups[1].Value;

                    string code = $@"
                            using System;
                            using System.Linq;
                            using System.Collections.Generic;

                            public class Evaluator
                            {{
                                public object Eval()
                                {{
                                    return {expression};
                                }}
                            }}";

                    var provider = new CSharpCodeProvider();
                    var parameters = new CompilerParameters
                    {
                        GenerateInMemory = true,
                        GenerateExecutable = false
                    };

                    parameters.ReferencedAssemblies.Add("System.dll");
                    parameters.ReferencedAssemblies.Add("System.Core.dll");
                    parameters.ReferencedAssemblies.Add("mscorlib.dll");

                    var compileResult = provider.CompileAssemblyFromSource(parameters, code);

                    if (compileResult.Errors.HasErrors)
                    {
                        result.Append($"[Error: {compileResult.Errors[0].ErrorText}]");
                    }
                    else
                    {
                        try
                        {
                            var type = compileResult.CompiledAssembly.GetType("Evaluator");
                            var instance = Activator.CreateInstance(type);
                            var method = type.GetMethod("Eval");
                            var evalResult = method.Invoke(instance, null);

                            result.Append(evalResult?.ToString() ?? "");
                        }
                        catch (Exception ex)
                        {
                            result.Append($"[Error: {ex.Message}]");
                        }
                    }

                    lastIndex = match.Index + match.Length;
                }

                result.Append(input.Substring(lastIndex));
                return result.ToString();
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{ex.Message}");
                Debug.WriteLine($"VSIXProject1Package: Error: {ex.Message}. Stack trace: {ex.StackTrace}");
                return ex.Message;
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        public void EvaluateSqlStatement(SqlShortcuts sqlShortcut)
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                var evaluatedExpression = EvaluateInlineCSharp(sqlShortcut.SqlComplete);

                // was there a table\view node selected
                if (!string.IsNullOrWhiteSpace(VSIX_DemoPackage._smoObjectExplorer.SelectedSmoNode))
                {
                    evaluatedExpression = Regex.Replace(
                        evaluatedExpression,
                        "<table>",
                        VSIX_DemoPackage._smoObjectExplorer.SelectedSmoNode,
                        RegexOptions.IgnoreCase
                    );
                }

                MessageBoxAdv.Show(evaluatedExpression, Resource.Grid_Eval_Header, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Log the error
                SimpleLogger.Error(ex.ToString());
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }
    }
}
