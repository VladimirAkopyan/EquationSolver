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

//======================================================================
// Class File: LinearEquationParser.cs
// Author: Bill Hallahan
// Date: April 13, 2010
//======================================================================

using System;
using System.Text;
using SparseCollections;

namespace Mathematics
{
    public enum LinearEquationParserStatus
    {
        Success,
        SuccessNoEquation,
        ErrorIllegalEquation,
        ErrorNoEqualSign,
        ErrorMultipleEqualSigns,
        ErrorNoTermBeforeEqualSign,
        ErrorNoTermAfterEqualSign,
        ErrorNoTermEncountered,
        ErrorNoVariableInEquation,
        ErrorMultipleDecimalPoints,
        ErrorTooManyDigits,
        ErrorMissingExponent,
        ErrorIllegalExponent,
    }

    internal enum LinearEquationParserState
    {
        ParseTerm,
        ParseOperator
    };

    /// <summary>
    /// This class provides a parser for strings that contain a system
    /// of linear equations that constructs the matrix equations needed
    /// to solve the system of equations.
    /// </summary>
    public class LinearEquationParser
    {
        private static readonly int m_maximumNumberLength = 20;

        private int m_startPosition;
        private int m_equationIndex;
        private LinearEquationParserState m_parserState;
        private bool m_negativeOperatorFlag;
        private bool m_equalSignInEquationFlag;
        private bool m_atLeastOneVariableInEquationFlag;
        private bool m_termBeforeEqualSignExistsFlag;
        private bool m_termAfterEqualSignExistsFlag;


        /// <summary>
        /// This property returns the last status value of the parser.
        /// </summary>
        /// <returns>A value of type LinearEquationParserStatus</returns>
        public LinearEquationParserStatus LastStatusValue
        {
            get;
            set;
        }

        /// <summary>
        /// This property gets the position in the input line where the last
        /// error occurred.  This should only be invoked if the Parser() method
        /// returns an error status value.
        /// </summary>
        /// <returns>The position in the input line where an error occurred</returns>
        public int ErrorPosition
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public LinearEquationParser()
        {
            Reset();
        }


        /// <summary>
        /// Destructor
        /// </summary>
        ~LinearEquationParser()
        {
        }

        /// <summary>
        /// This function parses line that contains all or part of a simple
        /// linear equation. The equation contains terms separated by operators.
        /// The term can be a number, a variable, or a number and a variable.
        /// A term cannot be split between lines input to the parser method.
        /// The operators are either the plus character '+', the minus
        /// character '-', or the equal sign character '='.  Numbers can have
        /// up to 15 digits, a decimal point, and an exponent of a power
        /// of 10 that follows the '^' character.
        /// </summary>
        /// <param name="inputLine">The input line of text to be parsed</param>
        /// <param name="aMatrix">The A matrix for the simultaneous equations.
        /// This is updated as each line of input is parsed.
        /// </param>
        /// <param name="bVector">The B vector for the simultaneous equations.
        /// This is updated as each line of input is parsed.</param>
        /// <param name="variableNameIndexMap">A map that stores the integer
        /// index for a variable using the variable name as a key.</param>
        /// <param name="numberOfEquations">A reference to an integer that
        /// will contain the  number of equations when this method exits.
        /// </param>
        /// <returns>An enum of type LinearEquationParserStatus</returns>
        public LinearEquationParserStatus Parse(string inputLine,
                                                Sparse2DMatrix<int, int, double> aMatrix,
                                                SparseArray<int, double> bVector,
                                                SparseArray<string, int> variableNameIndexMap,
                                                ref int numberOfEquations)
        {
            //------------------------------------------------------------------
            // Trim any space characters from the end of the line.
            //------------------------------------------------------------------

            inputLine.TrimEnd(null);

            //------------------------------------------------------------------
            // Assume success status.
            //------------------------------------------------------------------

            int positionIndex = 0;
            SetLastStatusValue(LinearEquationParserStatus.Success, positionIndex);

            //------------------------------------------------------------------
            // Skip whitespace characters
            //------------------------------------------------------------------

            SkipSpaces(inputLine, ref positionIndex);

            //------------------------------------------------------------------
            // Save the position of the first non-whitespace character. If
            // the first term is not found at this position then set the
            // error status to report that it is likely that the last
            // equation was not properly terminated.
            //------------------------------------------------------------------

            m_startPosition = positionIndex;

            //------------------------------------------------------------------
            // Separate the input string into tokens.
            //
            // Variables contains the letters A through Z and the underscore
            // '_' character.
            //
            // Operators include plus '+', minus '-', and times '*'.
            //
            // Delimiters include the equals sign '='.
            //
            // Numbers include the digits 0 through 9, the decimal point
            // (period character) '.', an optional exponent character which
            // is the letter '^', and up to two digits for the optional exponent.
            //
            // Check for sequences of terms and operators.
            //
            // Term:
            //   <Space> <Sign> Number <Space>
            //   <Space> <Sign> Variable <Space>
            //   <Space> <Sign> Number Variable <Space>
            //
            // Operator:
            //   <Space> Plus <Space>
            //   <Space> Minus <Space>
            //   <Space> Equals <Space>
            //
            //--------------------------------------------------------------

            bool operatorFoundLast = false;

            while (positionIndex < inputLine.Length)
            {
                if (m_parserState == LinearEquationParserState.ParseTerm)
                {
                    //------------------------------------------------------
                    // Skip whitespace characters
                    //------------------------------------------------------

                    SkipSpaces(inputLine, ref positionIndex);

                    if (positionIndex < inputLine.Length)
                    {
                        if (GetTerm(inputLine,
                                    ref positionIndex,
                                    aMatrix,
                                    bVector,
                                    variableNameIndexMap))
                        {
                            m_parserState = LinearEquationParserState.ParseOperator;
                            operatorFoundLast = false;
                        }
                        else
                        {
                            if (LastStatusValue == LinearEquationParserStatus.Success)
                            {
                                SetLastStatusValue(LinearEquationParserStatus.ErrorIllegalEquation,
                                                   positionIndex);
                            }

                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else if (m_parserState == LinearEquationParserState.ParseOperator)
                {
                    //------------------------------------------------------
                    // Skip whitespace characters
                    //------------------------------------------------------

                    SkipSpaces(inputLine, ref positionIndex);

                    if (positionIndex < inputLine.Length)
                    {
                        //------------------------------------------------------
                        // Check for plus sign, minus sign, or an equal sign.
                        //------------------------------------------------------

                        if (GetOperator(inputLine, ref positionIndex))
                        {
                            m_parserState = LinearEquationParserState.ParseTerm;
                            operatorFoundLast = true;
                        }
                        else
                        {
                            if (LastStatusValue != LinearEquationParserStatus.Success)
                            {
                                if (positionIndex == m_startPosition)
                                {
                                    SetLastStatusValue(LinearEquationParserStatus.ErrorIllegalEquation,
                                                       positionIndex);
                                }
                            }

                            break;
                        }
                    }
                }
            }

            // If an operator was found at 
            if ((positionIndex >= inputLine.Length) && (positionIndex > 0) && (!operatorFoundLast))
            {
                ResetForNewEquation();
                numberOfEquations = m_equationIndex;
            }

            return LastStatusValue;
        }

        /// <summary>
        /// This function resets the parser to its initial state for parsing
        /// a new set of simultaneous linear equations.
        /// </summary>
        void Reset()
        {
            m_startPosition = 0;
            ErrorPosition = 0;
            LastStatusValue =  LinearEquationParserStatus.Success;
            m_negativeOperatorFlag = false;
            m_equalSignInEquationFlag = false;
            m_atLeastOneVariableInEquationFlag = false;
            m_termBeforeEqualSignExistsFlag = false;
            m_termAfterEqualSignExistsFlag = false;
            m_parserState = LinearEquationParserState.ParseTerm;
            m_equationIndex = 0;
        }

        /// <summary>
        /// This function calculate the status value for an incomplete equation.
        /// This should be called if the IsCompleteEquation() method returns false.
        /// </summary>
        /// <returns>An enum value of type 'LinearEquationParserStatus'</returns>
        private LinearEquationParserStatus GetEquationStatus()
        {
            LinearEquationParserStatus status = LinearEquationParserStatus.Success;

            if ((!m_equalSignInEquationFlag)
                && (!m_termBeforeEqualSignExistsFlag)
                && (!m_termAfterEqualSignExistsFlag)
                && (!m_atLeastOneVariableInEquationFlag))
            {
                status = LinearEquationParserStatus.SuccessNoEquation;
            }
            else if (!m_equalSignInEquationFlag)
            {
                status = LinearEquationParserStatus.ErrorNoEqualSign;
            }
            else if (!m_termBeforeEqualSignExistsFlag)
            {
                status = LinearEquationParserStatus.ErrorNoTermBeforeEqualSign;
            }
            else if (!m_termAfterEqualSignExistsFlag)
            {
                status = LinearEquationParserStatus.ErrorNoTermAfterEqualSign;
            }
            else if (!m_atLeastOneVariableInEquationFlag)
            {
                status = LinearEquationParserStatus.ErrorNoVariableInEquation;
            }
            else
            {
                status = LinearEquationParserStatus.Success;
            }

            return status;
        }


        /// <summary>
        /// This function resets the parser to process a new equation.
        /// </summary>
        private void ResetForNewEquation()
        {
            m_startPosition = 0;
            m_negativeOperatorFlag = false;
            m_equalSignInEquationFlag = false;
            m_atLeastOneVariableInEquationFlag = false;
            m_termBeforeEqualSignExistsFlag = false;
            m_termAfterEqualSignExistsFlag = false;
            m_parserState = LinearEquationParserState.ParseTerm;
            m_equationIndex++;
        }

        /// <summary>
        /// This method gets a term in the simultaneous equation. The term
        /// can be a number, a variable, or a number and a variable. A term
        /// cannot be split between lines input to this method.
        /// </summary>
        /// <param name="inputLine">The input line to be parsed</param>
        /// <param name="positionIndex">The current parse position in the input string.</param>
        /// <param name="aMatrix">The A matrix for the simultaneous equations.
        /// This is updated as each line of input is parsed.
        /// </param>
        /// <param name="bVector">The B vector for the simultaneous equations.
        /// This is updated as each line of input is parsed.</param>
        /// <param name="variableNameIndexMap">A map that stores the integer
        /// index for a variable using the variable name as a key.</param>
        /// <returns>This method returns the value 'true' if and only if a term is found.
        /// </returns>
        private bool GetTerm(string inputLine,
                             ref int positionIndex,
                             Sparse2DMatrix<int, int, double> aMatrix,
                             SparseArray<int, double> bVector,
                             SparseArray<string, int> variableNameIndexMap)
        {
            //------------------------------------------------------------------
            // A term is one of the following three patterns.
            //
            // <Space> <Sign> Number <Space>
            // <Space> <Sign> Variable <Space>
            // <Space> <Sign> Number Variable <Space>
            //
            // Check for a plus or a minus sign at the start of a term.
            //------------------------------------------------------------------

            bool numberIsNegativeFlag = false;

            GetSign(inputLine,
                    ref positionIndex,
                    ref numberIsNegativeFlag);

            //------------------------------------------------------------------
            // Skip whitespace characters
            //------------------------------------------------------------------

            SkipSpaces(inputLine, ref positionIndex);

            //------------------------------------------------------------------
            // Check to see if this is a number or a variable.
            //------------------------------------------------------------------

            string numberString = "";

            bool haveNumberFlag = GetNumber(inputLine,
                                            ref positionIndex,
                                            ref numberString);

            //------------------------------------------------------------------
            // If an error occurred then abort.
            //------------------------------------------------------------------

            if (LastStatusValue != LinearEquationParserStatus.Success)
            {
                return false;
            }

            //------------------------------------------------------------------
            // If there was a number encountered then test to see if the
            // number has an exponent.
            //------------------------------------------------------------------

            if (haveNumberFlag)
            {
                if (positionIndex < inputLine.Length)
                {
                    //----------------------------------------------------------
                    // Does the number have an exponent?
                    //----------------------------------------------------------

                    if (inputLine[positionIndex] == '^')
                    {
                        positionIndex++;

                        //------------------------------------------------------
                        // Does the exponent have a sign.
                        //------------------------------------------------------

                        bool negativeExponentFlag = false;

                        GetSign(inputLine,
                                ref positionIndex,
                                ref negativeExponentFlag);

                        //------------------------------------------------------
                        // Get the exponent digits.
                        //------------------------------------------------------

                        string exponentString = "";

                        if (GetNumber(inputLine,
                                      ref positionIndex,
                                      ref exponentString))
                        {
                            //--------------------------------------------------
                            // Is the exponent a valid exponent.
                            //--------------------------------------------------

                            int exponentLength = exponentString.Length;

                            if (exponentLength <= 2)
                            {
                                bool exponent_error_flag = false;

                                for (int i = 0; i < exponentLength; ++i)
                                {
                                    if (!Char.IsDigit(exponentString[i]))
                                    {
                                        exponent_error_flag = true;
                                    }
                                }

                                if (!exponent_error_flag)
                                {
                                    numberString += 'E';

                                    if (negativeExponentFlag)
                                    {
                                        numberString += '-';
                                    }

                                    numberString += exponentString;
                                }
                                else
                                {
                                    SetLastStatusValue(LinearEquationParserStatus.ErrorIllegalExponent,
                                                       positionIndex);
                                    return false;
                                }
                            }
                            else
                            {
                                SetLastStatusValue(LinearEquationParserStatus.ErrorIllegalExponent,
                                                   positionIndex);
                                return false;
                            }
                        }
                        else
                        {
                            SetLastStatusValue(LinearEquationParserStatus.ErrorMissingExponent,
                                               positionIndex);
                            return false;
                        }
                    }
                }
            }

            //------------------------------------------------------------------
            // Skip whitespace characters
            //------------------------------------------------------------------

            SkipSpaces(inputLine, ref positionIndex);

            string variableName = "";

            bool haveVariableNameFlag = GetVariableName(inputLine,
                                                        ref positionIndex,
                                                        ref variableName);

            //------------------------------------------------------------------
            // Calculate the sign of the value. The sign is negated
            // if the equals sign has been encountered.
            //------------------------------------------------------------------

            bool negativeFlag =
                m_equalSignInEquationFlag ^ m_negativeOperatorFlag ^ numberIsNegativeFlag;

            double value = 0.0;

            if (haveNumberFlag)
            {
                value = Convert.ToDouble(numberString);

                if (negativeFlag)
                {
                    value = -value;
                }
            }
            else
            {
                value = 1.0;

                if (negativeFlag)
                {
                    value = -value;
                }
            }

            //------------------------------------------------------------------
            // If a variable was encountered then add to the aMatrix.
            //------------------------------------------------------------------

            bool haveTermFlag = false;

            if (haveVariableNameFlag)
            {
                //--------------------------------------------------------------
                // If either a number or a variable is found then a term was
                // found.
                //--------------------------------------------------------------

                haveTermFlag = true;

                //--------------------------------------------------------------
                // Each equation must have at least one variable.
                // Record that a variable was encountered in this equation.
                //--------------------------------------------------------------

                m_atLeastOneVariableInEquationFlag = true;

                //--------------------------------------------------------------
                // If this variable has not been encountered before then add
                // the variable to the variableNameIndexMap.
                //--------------------------------------------------------------

                int variableNameIndex = 0;

                if (!variableNameIndexMap.TryGetValue(variableName, out variableNameIndex))
                {
                    // This is a new variable. Add it to the map.
                    variableNameIndex = variableNameIndexMap.Count;
                    variableNameIndexMap[variableName] = variableNameIndex;
                }

                aMatrix[m_equationIndex, variableNameIndex] =
                    aMatrix[m_equationIndex, variableNameIndex] + value;
            }
            else if (haveNumberFlag)
            {
                //--------------------------------------------------------------
                // If either a number or a variable is found then a term was
                // found.
                //--------------------------------------------------------------
                
                haveTermFlag = true;

                //--------------------------------------------------------------
                // Put the value in the B vector.
                //--------------------------------------------------------------

                bVector[m_equationIndex] = bVector[m_equationIndex] - value;
            }
            else
            {
                haveTermFlag = false;
                SetLastStatusValue(LinearEquationParserStatus.ErrorNoTermEncountered,
                                   positionIndex);
                return false;
            }

            //------------------------------------------------------------------
            // There must be at least one term on each side of the equal sign.
            //------------------------------------------------------------------

            if (haveTermFlag)
            {
                if (m_equalSignInEquationFlag)
                {
                    m_termAfterEqualSignExistsFlag = true;
                }
                else
                {
                    m_termBeforeEqualSignExistsFlag = true;
                }
            }

            //------------------------------------------------------------------
            // Skip whitespace characters
            //------------------------------------------------------------------

            SkipSpaces(inputLine, ref positionIndex);

            return haveTermFlag;
        }

        /// <summary>
        /// This function parses a plus sign character or a minus sign character.
        /// </summary>
        /// <param name="inputLine">The input string to be parsed</param>
        /// <param name="positionIndex">A reference to the current parse position
        /// in the input string</param>
        /// <param name="negativeFlag">A reference to a boolean variable that is
        /// set to the value 'true' if and only if a minus sign is encountered.</param>
        /// <returns></returns>
        private bool GetSign(string inputLine,
                             ref int positionIndex,
                             ref bool negativeFlag)
        {
            //------------------------------------------------------------------
            // Check for a plus or a minus sign.
            //------------------------------------------------------------------

            bool haveSignFlag = false;
            negativeFlag = false;

            if (positionIndex < inputLine.Length)
            {
                char c = inputLine[positionIndex];

                if (c == '+')
                {
                    haveSignFlag = true;
                    positionIndex ++;
                }
                else if (c == '-')
                {
                    haveSignFlag = true;
                    negativeFlag = true;
                    positionIndex ++;
                }
            }

            return haveSignFlag;
        }

        /// <summary>
        /// This function parses a number string.
        /// </summary>
        /// <param name="inputLine">The input string to be parsed</param>
        /// <param name="positionIndex">A reference to the current parse position
        /// in the input string</param>
        /// <param name="numberString">A reference to a number string.</param>
        /// <returns>Returns the value 'true' if and only if a number is found.</returns>
        private bool GetNumber(string inputLine,
                               ref int positionIndex,
                               ref string numberString)
        {
            int decimalPointCount = 0;
            int digitLength = 0;
            bool haveNumberFlag = false;
            bool continueFlag = positionIndex < inputLine.Length;

            while (continueFlag)
            {
                Char c = inputLine[positionIndex];

                continueFlag = (c == '.');

                if (Char.IsDigit(c))
                {
                    if (++digitLength > m_maximumNumberLength)
                    {
                        SetLastStatusValue(LinearEquationParserStatus.ErrorTooManyDigits,
                                           positionIndex);
                        return false;
                    }

                    haveNumberFlag = true;
                    numberString += c;
                    positionIndex++;
                    continueFlag = positionIndex < inputLine.Length;
                }
                else
                {
                    continueFlag = c == '.';

                    if (continueFlag)
                    {
                        if (++decimalPointCount > 1)
                        {
                            SetLastStatusValue(LinearEquationParserStatus.ErrorMultipleDecimalPoints,
                                               positionIndex);
                            return false;
                        }

                        numberString += c;
                        positionIndex++;
                        continueFlag = positionIndex < inputLine.Length;
                    }
                }
            }

            if (numberString.Length > m_maximumNumberLength)
            {
                SetLastStatusValue(LinearEquationParserStatus.ErrorTooManyDigits,
                                   positionIndex);
                return false;
            }

            return haveNumberFlag;
        }

        /// <summary>
        /// This function parses a variable name string.
        /// </summary>
        /// <param name="inputLine">The input string to be parsed</param>
        /// <param name="positionIndex">A reference to the current parse position
        /// in the input string</param>
        /// <param name="variableName">A reference to a variable name string.</param>
        /// <returns>Returns the value 'true' if and only if a variable name is found.</returns>
        private bool GetVariableName(string inputLine,
                                     ref int positionIndex,
                                     ref string variableName)
        {
            bool haveVariableNameFlag = false;
            bool continueFlag = positionIndex < inputLine.Length;

            while (continueFlag)
            {
                Char c = inputLine[positionIndex];
                
                continueFlag = (Char.IsLetter(c) || c == '_');
                
                if (continueFlag)
                {
                    haveVariableNameFlag = true;
                    variableName += c;
                    positionIndex++;
                    continueFlag = positionIndex < inputLine.Length;
                }
            }
            
            return haveVariableNameFlag;
        }

        /// <summary>
        /// This function parses an operator string.
        /// </summary>
        /// <param name="inputLine">The input string to be parsed</param>
        /// <param name="positionIndex">A reference to the current parse position
        /// in the input string</param>
        /// <param name="numberString">A reference to a number string.</param>
        /// <returns>Returns the value 'true' if and only if an operator symbol is found.</returns>
        private bool GetOperator(string inputLine, ref int positionIndex)
        {
            //------------------------------------------------------------------
            // Skip whitespace characters
            //------------------------------------------------------------------
            
            SkipSpaces(inputLine, ref positionIndex);

            //------------------------------------------------------------------
            // Check for an equals sign.
            //------------------------------------------------------------------

            m_negativeOperatorFlag = false;
            bool haveEqualSignFlag = false;

            if (positionIndex < inputLine.Length)
            {
                if (inputLine[positionIndex] == '=')
                {
                    if (!m_equalSignInEquationFlag)
                    {
                        m_equalSignInEquationFlag = true;
                        haveEqualSignFlag = true;
                        positionIndex++;
                    }
                    else
                    {
                        SetLastStatusValue(LinearEquationParserStatus.ErrorMultipleEqualSigns,
                                           positionIndex);
                        return false;
                    }
                }
            }

            bool haveSignFlag = GetSign(inputLine,
                                        ref positionIndex,
                                        ref m_negativeOperatorFlag);

            return haveSignFlag || haveEqualSignFlag;
        }

        /// <summary>
        /// This method skips spaces in the input string.
        /// </summary>
        /// <param name="inputLine">The input string to be parsed</param>
        /// <param name="positionIndex">A reference to the current parse position
        /// in the input string</param>
        private void SkipSpaces(string inputLine, ref int positionIndex)
        {
            bool continueFlag = positionIndex < inputLine.Length;
            
            while (continueFlag)
            {
                char c = inputLine[positionIndex];
                
                continueFlag = Char.IsWhiteSpace(c);
                
                if (continueFlag)
                {
                    positionIndex++;
                    continueFlag = positionIndex < inputLine.Length;
                }
            }
        }

        /// <summary>
        /// This method sets the status value and saves the current parse position.
        /// </summary>
        /// <param name="status">The status value</param>
        /// <param name="positionIndex">The current parse position in the input
        /// string</param>
        private void SetLastStatusValue(LinearEquationParserStatus status,
                                        int positionIndex)
        {
            LastStatusValue = status;
            ErrorPosition = positionIndex;
        }
    }
}
