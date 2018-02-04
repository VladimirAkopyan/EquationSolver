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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Resources;
using System.Drawing.Printing;
using SparseCollections;
using Mathematics;

namespace csEquationSolver
{
    public partial class EquationsSolverForm : Form
    {
        private static readonly string m_defaultFileName = "Equations.txt";
        private string m_documentPathFileName;
        int m_printPageStartLine;
        private bool m_dirty;

        // Variables for the open and save file dialogs.
        private string m_dialogFilter;
        private int m_dialogFilterIndex;

        /// <summary>
        /// Printing variables.
        /// </summary>
        private PrintDocument m_printDocument;
        private PageSettings m_pageSettings;
        private PrinterSettings m_printerSettings;

        /// <summary>
        /// Constructor
        /// </summary>
        public EquationsSolverForm(string pathFileName)
        {
            InitializeComponent();

            m_documentPathFileName = pathFileName;
            m_printPageStartLine = 0;
            m_dirty = false;

            m_dialogFilter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            m_dialogFilterIndex = 0;

            // Object to cache page settings
            m_pageSettings = new PageSettings();

            // Data member for printing and print event handler.
            m_printDocument = new PrintDocument();
            m_printDocument.BeginPrint += new PrintEventHandler(this.OnBeginPrint);
            m_printDocument.PrintPage += new PrintPageEventHandler(this.OnPrintPage);

            // Object to cache printer settings.
            m_printerSettings = new PrinterSettings();
        }

        // Event handlers
        private void EquationsSolverForm_Load(object sender, EventArgs e)
        {
            // Set the form size, location, and state.
            this.Size = Properties.Settings.Default.FormSize;
            this.Location = Properties.Settings.Default.FormLocation;
            this.WindowState = Properties.Settings.Default.FormWindowState;

            if (!string.IsNullOrEmpty(m_documentPathFileName))
            {
                LoadDocumentFromFile(m_documentPathFileName);
            }
        }

        private void EquationsSolverForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_printDocument.PrintPage -= new PrintPageEventHandler(this.OnPrintPage);
            m_printDocument.BeginPrint -= new PrintEventHandler(this.OnBeginPrint);

            // Save the form state, size, and location.
            Properties.Settings.Default.FormWindowState = this.WindowState;

            if (this.WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.FormSize = this.Size;
                Properties.Settings.Default.FormLocation = this.Location;
            }
            else
            {
                Properties.Settings.Default.FormSize = this.RestoreBounds.Size;
                Properties.Settings.Default.FormLocation = this.RestoreBounds.Location;
            }

            Properties.Settings.Default.Save();
        }

        private void EquationsSolverForm_Resize(object sender, EventArgs e)
        {
            ResizeMainFormControls();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            New();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void pageSetupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PageSetup();
        }

        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrintPreview();
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Print();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit();
        }

        private void cutCtrlXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Cut();
        }

        private void copyCtrlCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void pasteCtrlVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Paste();
        }

        private void selectAllCtrlAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectAll();
        }

        private void solveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Solve();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Properties.Resources.IDS_ABOUT_BOX_TEXT,
                            Properties.Resources.IDS_ABOUT_BOX_CAPTION);
        }

        private void equationsRichTextBox_TextChanged(object sender, EventArgs e)
        {
            m_dirty = true;
        }

        // OnBeginPrint 
        private void OnBeginPrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            // Start with the first line in the rich-text box.
            m_printPageStartLine = 0;
        }

        /// <summary>
        /// Handler to print a page.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The print event arguments</param>
        private void OnPrintPage(object sender, PrintPageEventArgs e)
        {
            // Render here using m_pageSettings for page parameters.

            float linesPerPage = 0;
            float yPos = 0;
            int count = 0;
            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top;
            String line = null;

            using (Font font = new Font("Courier New", 12))
            {
                // Calculate the number of lines per page.
                int height = m_pageSettings.PaperSize.Height - (m_pageSettings.Margins.Bottom + m_pageSettings.Margins.Top);
                linesPerPage = height / font.GetHeight(e.Graphics);
                bool hasMorePages = true;

                // Iterate over the file, printing each line. 
                while (count < linesPerPage)
                {
                    // If past the last line in the rich-text control, then exit.
                    if (((m_printPageStartLine + count) >= equationsRichTextBox.Lines.Count())
                        || ((line = equationsRichTextBox.Lines[m_printPageStartLine + count]) == null))
                    {
                        hasMorePages = false;
                        break;
                    }

                    yPos = topMargin + (count * font.GetHeight(e.Graphics));
                    e.Graphics.DrawString(line,
                                          font,
                                          Brushes.Black,
                                          leftMargin,
                                          yPos,
                                          new StringFormat());
                    // This does't work because m_pageSettings isn't always set.  The printing code
                    // is a work-in-progress.
                    //e.Graphics.DrawString(line, font, Brushes.Black, m_pageSettings.PrintableArea);
                    ++count;
                }

                // Accumulate the count so that the next page starts at the current line in the rich-text box.
                m_printPageStartLine += count;

                // If more lines exist, print another page. 
                e.HasMorePages = hasMorePages;
            }
        }

        protected override void OnKeyDown(KeyEventArgs keyEvent)
        {
            // Determine the key that is depressed along with the Control key.
            Keys key = keyEvent.KeyData;

            if (key == Keys.A)
            {
                SelectAll();
            }
            else if (key == Keys.C)
            {
                Copy();
            }
            else if (key == Keys.P)
            {
                Paste();
            }
            else if (key == Keys.X)
            {
                Cut();
            }
        }

        // This was added by the automatic form designer. Perhaps someday I will use it.
        private void mainToolStripStatusLabel_Click(object sender, EventArgs e)
        {
        }

        // End of handlers

        /// <summary>
        /// This method resizes and repositions form controls when the main window is resized.
        /// </summary>
        private void ResizeMainFormControls()
        {
            int x = mainMenuStrip.Location.X;
            int y = mainMenuStrip.Location.Y + mainMenuStrip.Height;
            equationsRichTextBox.Location = new System.Drawing.Point(x, y);
            int cx = this.ClientSize.Width;
            int cy = this.ClientSize.Height - mainMenuStrip.Height - mainStatusStrip.Height;
            equationsRichTextBox.Size = new System.Drawing.Size(cx, cy);
        }

        /// <summary>
        /// This methods tests whether data is present.
        /// </summary>
        /// <returns>'true' if and only if data is present</returns>
        public bool HaveData()
        {
            return !string.IsNullOrEmpty(equationsRichTextBox.Text);
        }

        /// <summary>
        /// This methods tests whether data is present.
        /// </summary>
        public void New()
        {
            AvoidLosingData();
            equationsRichTextBox.Text = "";
            mainToolStripStatusLabel.Text = "";
            m_documentPathFileName = "";
            m_dirty = false;
        }

        /// <summary>
        /// Method to avoid losing data. If data is present and it
        /// has not been saved, then prompt the user to save the data.
        /// </summary>
        private void AvoidLosingData()
        {
            // If data is present and it has not been saved,
            // then prompt the user to save the data.
            if (HaveData() && (m_dirty))
            {
                DialogResult result = MessageBox.Show(
                                          Properties.Resources.IDS_DO_YOU_WANT_TO_SAVE_QUERY,
                                          Properties.Resources.IDS_SAVE_ON_EXIT_PROMPT,
                                          MessageBoxButtons.YesNo,
                                          MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Save();
                }
            }
        }

        /// <summary>
        /// Load the file.
        /// </summary>
        /// <param name="pathFileName">The path and file name</param>
        private void LoadDocumentFromFile(string pathFileName)
        {
            // Open and read the file into the rich text control.
            equationsRichTextBox.LoadFile(pathFileName, RichTextBoxStreamType.PlainText);

            m_documentPathFileName = pathFileName;
            m_dirty = false;
        }

        /// <summary>
        /// Save the file.
        /// </summary>
        /// <param name="pathFileName">The path and file name</param>
        private void SaveDocumentToFile(string pathFileName)
        {
            // Save the data in the rich text control to the file.
            equationsRichTextBox.SaveFile(pathFileName, RichTextBoxStreamType.PlainText);

            m_dirty = false;
        }

        /// <summary>
        /// Launch the open file dialog and open a file.
        /// </summary>
        private void Open()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = m_dialogFilter;

            openFileDialog.FilterIndex = m_dialogFilterIndex;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadDocumentFromFile(openFileDialog.FileName);
            }
        }

        /// <summary>
        /// Save the document to a file. If the document currently
        /// has no name then call the SaveAs method to prompt for a file name.
        /// </summary>
        private void Save()
        {
            // Is there anything to save?
            if (HaveData())
            {
                if (!string.IsNullOrEmpty(m_documentPathFileName))
                {
                    SaveDocumentToFile(m_documentPathFileName);
                }
                else
                {
                    SaveAs();
                }
            }
            else
            {
                MessageBox.Show(Properties.Resources.IDS_NOTHING_TO_SAVE);
            }
        }

        /// <summary>
        /// Prompt for a file name and save the file.
        /// </summary>
        private void SaveAs()
        {
            // Is there anything to save?
            if (HaveData())
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = m_dialogFilter;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SaveDocumentToFile(saveFileDialog.FileName);

                    m_documentPathFileName = saveFileDialog.FileName;
                }
            }
            else
            {
                MessageBox.Show(Properties.Resources.IDS_NOTHING_TO_SAVE);
            }
        }

        /// <summary>
        /// Method to setup the page for printing.
        /// </summary>
        private void PageSetup()
        {
            PageSetupDialog pageSetupDialog = new PageSetupDialog();
            pageSetupDialog.PageSettings = m_pageSettings;
            pageSetupDialog.PrinterSettings = m_printerSettings;
            pageSetupDialog.AllowOrientation = true;
            pageSetupDialog.AllowMargins = true;
            pageSetupDialog.ShowDialog();
        }

        /// <summary>
        /// Methods to preview what will be printed.
        /// </summary>
        private void PrintPreview()
        {
            PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog();
            printPreviewDialog.Document = m_printDocument;
            printPreviewDialog.ShowDialog();
        }

        /// <summary>
        /// This method launches a print dialog and if the user clicks
        /// the OK button, the document is printed.
        /// </summary>
        private void Print()
        {
            // Is there anything to print?
            if (HaveData())
            {
                if (string.IsNullOrEmpty(m_documentPathFileName))
                {
                    m_documentPathFileName = m_defaultFileName;
                }

                // Set the page settings to use.
                m_printDocument.DefaultPageSettings = m_pageSettings;
                // Set the document name.
                m_printDocument.DocumentName = m_documentPathFileName;

                // Launch the print dialog.
                PrintDialog printDialog = new PrintDialog();
                printDialog.Document = m_printDocument;

                // Setting UseEXDialog is necessary for Windows 7 and 64-bit versions of Windows.
                // Without this, the dialog won't show up and will immediately return DialogResult.Cancel
                printDialog.UseEXDialog = true;

                if (printDialog.ShowDialog() == DialogResult.OK)
                {
                    m_printDocument.Print();
                }
            }
            else
            {
                MessageBox.Show(Properties.Resources.IDS_NOTHING_TO_PRINT);
            }
        }

        /// <summary>
        /// Method to cause the application to close.
        /// </summary>
        private void Exit()
        {
            AvoidLosingData();
            Application.Exit();
        }

        /// <summary>
        /// This method cuts text in the equationRichTextBox control.
        /// </summary>
        private void Cut()
        {
            equationsRichTextBox.Cut();
        }

        /// <summary>
        /// This method copies text in the equationRichTextBox control.
        /// </summary>
        private void Copy()
        {
            equationsRichTextBox.Copy();
        }

        /// <summary>
        /// This method pastes text into the equationRichTextBox control.
        /// </summary>
        private void Paste()
        {
            equationsRichTextBox.Paste();
        }

        /// <summary>
        /// This method selects all text in the equationRichTextBox control.
        /// </summary>
        private void SelectAll()
        {
            equationsRichTextBox.SelectAll();
        }

        private void Solve()
        {
            mainToolStripStatusLabel.Text = Properties.Resources.IDS_SOLVING_EQUATIONS;

            Sparse2DMatrix<int, int, double> aMatrix = new Sparse2DMatrix<int, int, double>();
            SparseArray<int, double> bVector = new SparseArray<int, double>();
            SparseArray<string, int> variableNameIndexMap = new SparseArray<string, int>();
            int numberOfEquations = 0;

            LinearEquationParser parser = new LinearEquationParser();
            LinearEquationParserStatus parserStatus = LinearEquationParserStatus.Success;

            foreach (string inputLine in equationsRichTextBox.Lines)
            {
                parserStatus = parser.Parse(inputLine,
                                            aMatrix,
                                            bVector,
                                            variableNameIndexMap,
                                            ref numberOfEquations);

                if (parserStatus != LinearEquationParserStatus.Success)
                {
                    break;
                }
            }

            // Assume success.
            string mainStatusBarText = Properties.Resources.IDS_EQUATIONS_SOLVED;

            // Did an error occur?
            if (parserStatus == LinearEquationParserStatus.Success)
            {
                // Are there the same number of equations as variables?
                if (numberOfEquations == variableNameIndexMap.Count)
                {
                    // Create a solution vector.
                    SparseArray<int, double> xVector = new SparseArray<int, double>();

                    // Solve the simultaneous equations.
                    LinearEquationSolverStatus solverStatus =
                        LinearEquationSolver.Solve(numberOfEquations,
                                                   aMatrix,
                                                   bVector,
                                                   xVector);

                    if (solverStatus == LinearEquationSolverStatus.Success)
                    {
                        string solutionString = "";

                        foreach (KeyValuePair<string, int> pair in variableNameIndexMap)
                        {
                            solutionString += string.Format("{0} = {1}", pair.Key, xVector[pair.Value]);
                            solutionString += "\n";
                        }

                        equationsRichTextBox.Text += "\n" + solutionString;
                    }
                    else if (solverStatus == LinearEquationSolverStatus.IllConditioned)
                    {
                        mainStatusBarText = Properties.Resources.IDS_ILL_CONDITIONED_SYSTEM_OF_EQUATIONS;
                    }
                    else if (solverStatus == LinearEquationSolverStatus.Singular)
                    {
                        mainStatusBarText = Properties.Resources.IDS_SINGULAR_SYSTEM_OF_EQUATIONS;
                    }
                }
                else if (numberOfEquations < variableNameIndexMap.Count)
                {
                    mainStatusBarText = string.Format(Properties.Resources.IDS_TOO_FEW_EQUATIONS,
                                                      numberOfEquations, variableNameIndexMap.Count);
                }
                else if (numberOfEquations > variableNameIndexMap.Count)
                {
                    mainStatusBarText = string.Format(Properties.Resources.IDS_TOO_MANY_EQUATIONS,
                                                      numberOfEquations, variableNameIndexMap.Count);
                }
            }
            else
            {
                // An error occurred. Report the error in the status bar.
                mainStatusBarText = LinearEquationParserStatusInterpreter.GetStatusString(parserStatus);
            }

            mainToolStripStatusLabel.Text = mainStatusBarText;
        }
    }
}
