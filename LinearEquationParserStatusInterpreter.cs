//=======================================================================
// Copyright (C) 2010-2013 William Hallahan
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//=======================================================================

﻿using System;
using System.Text;
using Mathematics;

namespace csEquationSolver
{
    /// <summary>
    /// This class allows converting the status returned from the LinearEquationParser.Parse
    /// method.  This is done here to avoid having language-dependent resources used in the
    /// LinearEquationParser class.
    /// </summary>
    public static class LinearEquationParserStatusInterpreter
    {
        /// <summary>
        /// This method returns a status string corresponding to the passed
        /// status value returned from the LinearEquationParser.Parse method.
        /// </summary>
        /// <param name="status">An status value of type LinearEquationParserStatus</param>
        /// <returns>The string corresponding to the passed status code.</returns>
        public static string GetStatusString(LinearEquationParserStatus status)
        {
            string statusString = "";

            switch (status)
            {
                case LinearEquationParserStatus.Success:
                case LinearEquationParserStatus.SuccessNoEquation:
                    statusString = Properties.Resources.IDS_SUCCESS;
                    break;
                case LinearEquationParserStatus.ErrorIllegalEquation:
                    statusString = Properties.Resources.IDS_ERROR_ILLEGAL_EQUATION;
                    break;
                case LinearEquationParserStatus.ErrorNoEqualSign:
                    statusString = Properties.Resources.IDS_ERROR_NO_EQUAL_SIGN;
                    break;
                case LinearEquationParserStatus.ErrorMultipleEqualSigns:
                    statusString = Properties.Resources.IDS_ERROR_MULTIPLE_EQUAL_SIGNS;
                    break;
                case LinearEquationParserStatus.ErrorNoTermBeforeEqualSign:
                    statusString = Properties.Resources.IDS_ERROR_NO_TERM_BEFORE_EQUAL_SIGN;
                    break;
                case LinearEquationParserStatus.ErrorNoTermAfterEqualSign:
                    statusString = Properties.Resources.IDS_ERROR_NO_TERM_AFTER_EQUAL_SIGN;
                    break;
                case LinearEquationParserStatus.ErrorNoTermEncountered:
                    statusString = Properties.Resources.IDS_ERROR_NO_TERM_ENCOUNTERED;
                    break;
                case LinearEquationParserStatus.ErrorNoVariableInEquation:
                    statusString = Properties.Resources.IDS_ERROR_NO_VARIABLE_IN_EQUATION;
                    break;
                case LinearEquationParserStatus.ErrorMultipleDecimalPoints:
                    statusString = Properties.Resources.IDS_ERROR_MULTIPLE_DECIMAL_POINTS;
                    break;
                case LinearEquationParserStatus.ErrorTooManyDigits:
                    statusString = Properties.Resources.IDS_ERROR_TOO_MANY_DIGITS;
                    break;
                case LinearEquationParserStatus.ErrorMissingExponent:
                    statusString = Properties.Resources.IDS_ERROR_MISSING_EXPONENT;
                    break;
                case LinearEquationParserStatus.ErrorIllegalExponent:
                    statusString = Properties.Resources.IDS_ERROR_ILLEGAL_EXPONENT;
                    break;
                default:
                    statusString = Properties.Resources.IDS_ERROR_ILLEGAL_EQUATION;
                    break;
            }

            return statusString;
        }
    }
}
