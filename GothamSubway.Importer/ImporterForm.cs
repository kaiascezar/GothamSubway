﻿using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace GothamSubway.Importer
{
    public partial class ImporterForm : Form
    {
        Excel.Workbook workbook;
        Excel.Worksheet worksheet;
        Excel.Application application;
        List<string> columns;
        List<List<string>> rows;
        private int _checkedRadioButton;

        public ImporterForm()
        {
            InitializeComponent();

            workbook = null;
            worksheet = null;
            application = null;
            _checkedRadioButton = 0;
        }

        private void btnLoadFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                bgwLoader.RunWorkerAsync(openFileDialog1.FileName);
        }

        private void btnSaveDB_Click(object sender, EventArgs e)
        {
            if (_checkedRadioButton <= 0 || _checkedRadioButton > 3) // 나중에 상수로 바꾸자
            {
                MessageBox.Show("저장 방식을 선택해 주세요");
                return;
            }

            bgwInsert.RunWorkerAsync(_checkedRadioButton);
        }
        private void GetExcelData(List<List<string>> r, List<string> w)
        {
            rows = new List<List<string>>();
            columns = new List<string>();

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                MessageBox.Show($"파일명 : \n{openFileDialog1.FileName}");

                application = new Excel.Application();
                workbook = application.Workbooks.Open(openFileDialog1.FileName);
                worksheet = workbook.Sheets[1];
                application.Visible = false;

                Range range = worksheet.UsedRange;

                for (int i = 1; i <= (range.Rows.Count > 100 ? 100 : range.Rows.Count); ++i)
                {
                    if (i != 1)
                        rows.Add(new List<string>());

                    for (int j = 1; j <= range.Columns.Count; ++j)
                    {
                        if (i == 1) // 첫째줄이면?
                        {
                            if (range.Cells[i, j] != null && range.Cells[i, j].Value2 != null)
                                columns.Add(((Range)range.Cells[i, j]).Value2.ToString());
                        }
                        else
                        {
                            if (range.Cells[i, j] != null && range.Cells[i, j].Value2 != null)
                                rows[i - 2].Add(((Range)range.Cells[i, j]).Value2.ToString());
                        }
                    }
                }

                ReleaseObject(range);
                ReleaseObject(worksheet);
                ReleaseObject(workbook);
                application.Quit();
                ReleaseObject(application);
            }
        }
        static void ReleaseObject(object obj)
        {
            try
            {
                if (obj != null)
                {
                    Marshal.ReleaseComObject(obj); // 액셀 객체 해제
                    obj = null;
                }
            }
            catch (Exception ex)
            {
                obj = null;
                throw ex;
            }
            finally
            {
                GC.Collect(); // 가비지 수집
            }
        }
        private void AddGridView(DataGridView view, List<string> c, List<List<string>> r)
        {
            view.Rows.Clear();
            view.Columns.Clear();

            foreach (string column in columns)
            {
                view.ColumnCount += 1;
                view.Columns[this.dgvViewer.Columns.Count - 1].HeaderText = column;
                view.Columns[this.dgvViewer.Columns.Count - 1].Name = column;
            }

            foreach (List<string> row in rows)
            {
                view.Rows.Add(row.ToArray());
            }
        }

        private void bgwLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            rows = new List<List<string>>();
            columns = new List<string>();

            application = new Excel.Application();
            workbook = application.Workbooks.Open((string)e.Argument);
            worksheet = workbook.Sheets[1];
            application.Visible = false;

            Range range = worksheet.UsedRange;

            for (int i = 1; i <= range.Rows.Count; ++i)
            {
                if (i != 1)
                    rows.Add(new List<string>());

                for (int j = 1; j <= range.Columns.Count; ++j)
                {
                    if (i == 1) // 첫째줄이면?
                    {
                        if (range.Cells[i, j] != null && range.Cells[i, j].Value2 != null)
                            columns.Add(((Range)range.Cells[i, j]).Value2.ToString());
                    }
                    else
                    {
                        if (range.Cells[i, j] != null && range.Cells[i, j].Value2 != null)
                            rows[i - 2].Add(((Range)range.Cells[i, j]).Value2.ToString());
                    }
                }

                bgwLoader.ReportProgress(0, i.ToString());
            }

            ReleaseObject(range);
            ReleaseObject(worksheet);
            ReleaseObject(workbook);
            application.Quit();
            ReleaseObject(application);
        }

        private void bgwLoader_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            lblProgress.Text = $"읽어들인 행 : {(string)e.UserState}";
        }

        private void bgwLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dgvViewer.Rows.Clear();
            dgvViewer.Columns.Clear();

            foreach (string column in columns)
            {
                dgvViewer.ColumnCount += 1;
                dgvViewer.Columns[dgvViewer.Columns.Count - 1].HeaderText = column;
                dgvViewer.Columns[dgvViewer.Columns.Count - 1].Name = column;
            }

            foreach (List<string> row in rows.GetRange(0,(rows.Count > 100 ? 100 : rows.Count)))
            {
                dgvViewer.Rows.Add(row.ToArray());
            }
        }

        private void bgwInsert_DoWork(object sender, DoWorkEventArgs e)
        {
            if((int)e.Argument == 1)
            {

            }
            else if((int)e.Argument == 2)
            {
                // Entity에 값 넣기
                List<Satisfaction> satisfactions = new List<Satisfaction>();
                List<SatisfactionCategory> satisfactionCategories = new List<SatisfactionCategory>();
                int maxFirstId;
                int beforeSatisfactionCategoryCount;

                using (var context = new GothamSubwayEntities())
                {
                    satisfactionCategories = context.SatisfactionCategories.ToList();
                    if (satisfactionCategories.Count != 0)
                        maxFirstId = (context.SatisfactionCategories.Max(x => x.SatisfactionCategoryId)) / 100;
                    else
                        maxFirstId = 1;
                    beforeSatisfactionCategoryCount = satisfactionCategories.Count;
                }

                for(int i = 0;i < rows.Count; ++i)
                {
                    // SatisfactionCategory
                    SatisfactionCategory firstSatisfactionCategory = new SatisfactionCategory();
                    // 카테고리1이 없을 떄 만들어주기
                    if (satisfactionCategories.Find(x => x.Item == rows[i][0]) == null)
                    {
                        firstSatisfactionCategory.Item = rows[i][0];
                        firstSatisfactionCategory.SatisfactionCategoryId = maxFirstId * 100;
                        maxFirstId += 1;
                        satisfactionCategories.Add(firstSatisfactionCategory);
                    }

                    int upperId = satisfactionCategories.Find(x => x.Item == rows[i][0]).SatisfactionCategoryId;

                    SatisfactionCategory secondSatisfactionCategory = new SatisfactionCategory();
                    // 카테고리2가 없을 때, 또는 카테고리2가 있는데 카테고리1이 다를 때
                    if (satisfactionCategories.Find(x => x.Item == rows[i][1]) == null ||
                        ((satisfactionCategories.Find(x => x.Item == rows[i][1]) != null) &&
                        satisfactionCategories.Find(x => x.Item == rows[i][1]).UpperId != upperId))
                    {
                        secondSatisfactionCategory.Item = rows[i][1];
                        secondSatisfactionCategory.UpperId = upperId;
                        if (satisfactionCategories.Find(x => x.UpperId == upperId) == null)
                            secondSatisfactionCategory.SatisfactionCategoryId = upperId + 1;
                        else
                            secondSatisfactionCategory.SatisfactionCategoryId =
                                satisfactionCategories.Where(x => x.UpperId == upperId).Max(x => x.SatisfactionCategoryId) + 1;
                        secondSatisfactionCategory.SatisfactionCategory2 = satisfactionCategories.Find(x => x.SatisfactionCategoryId == secondSatisfactionCategory.UpperId);
                        satisfactionCategories.Add(secondSatisfactionCategory);
                    }
                    else
                        secondSatisfactionCategory = satisfactionCategories.Find(x => x.Item == rows[i][1] && x.UpperId == upperId);

                    // Satisfaction
                    Satisfaction satisfaction = new Satisfaction() { SatisfactionCategory = secondSatisfactionCategory };
                    satisfaction.Excellent = decimal.Parse(rows[i][2]);
                    satisfaction.Good = decimal.Parse(rows[i][3]);
                    satisfaction.Soso = decimal.Parse(rows[i][4]);
                    satisfaction.Bad = decimal.Parse(rows[i][5]);
                    satisfaction.Terrible = decimal.Parse(rows[i][6]);

                    satisfactions.Add(satisfaction);
                }

                foreach(var s in satisfactionCategories)
                {
                    if (s.SatisfactionCategoryId == 0)
                        bgwInsert.ReportProgress(0, "error");
                }

                // Entity를 DB에 저장
                using(var context = new GothamSubwayEntities())
                {
                    for (int i = beforeSatisfactionCategoryCount; i < satisfactionCategories.Count; ++i)
                        context.SatisfactionCategories.Add(satisfactionCategories[i]);
                    context.Satisfactions.AddRange(satisfactions);
                    context.SaveChanges();
                }
            }
            else if((int)e.Argument == 3)
            {
                // Entity에 값 넣기
                List<Electricity> electricities = new List<Electricity>();

                for(int i = 0; i < rows.Count; ++i)
                {
                    Electricity electricity = new Electricity();

                    double dateNumber = 0;
                    dateNumber = double.Parse(rows[i][0]);
                    if (dateNumber > 60d)
                        dateNumber = dateNumber - 2;
                    else
                        dateNumber = dateNumber - 1;

                    DateTime dateTime = new DateTime(1900, 1, 1);
                    dateTime = dateTime.AddDays(dateNumber);

                    electricity.Month = dateTime;
                    electricity.Usage = int.Parse(rows[i][1]);
                    electricity.Bill = int.Parse(rows[i][2]);

                    electricities.Add(electricity);

                    bgwInsert.ReportProgress(0, i.ToString());
                }

                // Entity를 DB에 저장하기
                using(var context = new GothamSubwayEntities())
                {
                    //foreach (Electricity electricity in electricities)
                    context.Electricities.AddRange(electricities);
                    context.SaveChanges();
                }
            }
        }

        private void bgwInsert_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            lblProgress2.Text = $"변환된 행 : {(string)e.UserState}";
        }

        private void bgwInsert_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("완료");
        }

        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton checkedRadioButton = sender as RadioButton;
            _checkedRadioButton = int.Parse(checkedRadioButton.Tag.ToString());
        }
    }
}