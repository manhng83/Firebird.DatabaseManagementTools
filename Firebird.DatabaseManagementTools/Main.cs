using FirebirdSql.Data.FirebirdClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Firebird.DatabaseManagementTools
{
    public partial class Main : Form
    {
        private string connectionString = string.Empty;
        private FbConnection fbConnection;
        private FbCommand fbCommand;
        private FbDataReader fbDataReader;
        private string dbInfoFilePath = string.Empty;
        private string resultFilePath = string.Empty;

        //Controls
        private string tableName = string.Empty;

        private string columnName = string.Empty;

        public Main()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            connectionString = textBox1.Text.Trim();
            fbConnection = new FbConnection(connectionString);
            try
            {
                fbConnection.Open();
                MessageBox.Show("Connected");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
                throw;
            }
            finally
            {
                fbConnection.Close();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.IO.File.WriteAllText(dbInfoFilePath, connectionString, Encoding.UTF8);
        }

        private void Main_Load(object sender, EventArgs e)
        {
            dbInfoFilePath = Path.Combine(Environment.CurrentDirectory, "DBInfo.txt");
            resultFilePath = Path.Combine(Environment.CurrentDirectory, "Result.txt");

            if (!File.Exists(dbInfoFilePath))
            {
                File.WriteAllText(dbInfoFilePath, string.Empty, Encoding.UTF8);
            }

            if (!File.Exists(resultFilePath))
            {
                File.WriteAllText(resultFilePath, string.Empty, Encoding.UTF8);
            }

            string s = File.ReadAllText(dbInfoFilePath, Encoding.UTF8);
            if (!string.IsNullOrWhiteSpace(s))
            {
                textBox1.Text = s;
            }

            GetConnectionString();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            tableName = txtTableName.Text.Trim();
            string s = @"
SELECT RDB$RELATION_NAME AS TABLE_NAME, list(trim(RDB$FIELD_NAME),',') AS COLUMNS
FROM RDB$RELATIONS
LEFT JOIN (SELECT * FROM RDB$RELATION_FIELDS ORDER BY RDB$FIELD_POSITION) USING (rdb$relation_name)
WHERE
(RDB$RELATIONS.RDB$SYSTEM_FLAG IS null OR RDB$RELATIONS.RDB$SYSTEM_FLAG = 0)
AND RDB$RELATIONS.rdb$view_blr IS null
GROUP BY RDB$RELATION_NAME
ORDER BY 1
";
            string tblName = string.Empty;
            string columns = string.Empty;

            using (fbConnection = new FbConnection(connectionString))
            {
                using (fbCommand = new FbCommand(s, fbConnection))
                {
                    fbConnection.Open();
                    using (fbDataReader = fbCommand.ExecuteReader())
                    {
                        while (fbDataReader.Read())
                        {
                            // FbCommand
                            // http://www.firebirdfaq.org/faq348/
                            // https://csharp.hotexamples.com/examples/FirebirdSql.Data.Firebird/FbCommand/ExecuteReader/php-fbcommand-executereader-method-examples.html
                            if (!fbDataReader.IsDBNull(0))
                            {
                                tblName = fbDataReader["TABLE_NAME"].ToString();
                                //tblName = fbDataReader.GetValue(0).ToString();
                            }
                            if (!fbDataReader.IsDBNull(1))
                            {
                                columns = fbDataReader["COLUMNS"].ToString();
                                //columns = fbDataReader.GetValue(1).ToString();
                            }
                            sb.AppendLine($"{tblName.Trim()} {columns.Trim()}");
                        }
                    }
                }
            }

            MessageBox.Show("Done");
            SaveText(sb.ToString());
            OpenText();
        }

        private void GetConnectionString()
        {
            connectionString = textBox1.Text.Trim();
        }

        private void OpenText()
        {
            Process p = new Process();
            ProcessStartInfo psi = new ProcessStartInfo("Notepad.exe", resultFilePath);
            p.StartInfo = psi;
            p.Start();
        }

        private void SaveText(string s)
        {
            File.WriteAllText(resultFilePath, s, Encoding.UTF8);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            tableName = txtTableName.Text.Trim();
            string sql = @"
		SELECT
		  RF.RDB$FIELD_NAME FIELD_NAME,
		  CASE F.RDB$FIELD_TYPE
		    WHEN 7 THEN
		      CASE F.RDB$FIELD_SUB_TYPE
		        WHEN 0 THEN 'SMALLINT'
		        WHEN 1 THEN 'NUMERIC(' || F.RDB$FIELD_PRECISION || ', ' || (-F.RDB$FIELD_SCALE) || ')'
		        WHEN 2 THEN 'DECIMAL'
		      END
		    WHEN 8 THEN
		      CASE F.RDB$FIELD_SUB_TYPE
		        WHEN 0 THEN 'INTEGER'
		        WHEN 1 THEN 'NUMERIC('  || F.RDB$FIELD_PRECISION || ', ' || (-F.RDB$FIELD_SCALE) || ')'
		        WHEN 2 THEN 'DECIMAL'
		      END
		    WHEN 9 THEN 'QUAD'
		    WHEN 10 THEN 'FLOAT'
		    WHEN 12 THEN 'DATE'
		    WHEN 13 THEN 'TIME'
		    WHEN 14 THEN 'CHAR(' || (TRUNC(F.RDB$FIELD_LENGTH / CH.RDB$BYTES_PER_CHARACTER)) || ') '
		    WHEN 16 THEN
		      CASE F.RDB$FIELD_SUB_TYPE
		        WHEN 0 THEN 'BIGINT'
		        WHEN 1 THEN 'NUMERIC(' || F.RDB$FIELD_PRECISION || ', ' || (-F.RDB$FIELD_SCALE) || ')'
		        WHEN 2 THEN 'DECIMAL'
		      END
		    WHEN 27 THEN 'DOUBLE'
		    WHEN 35 THEN 'TIMESTAMP'
		    WHEN 37 THEN 'VARCHAR(' || (TRUNC(F.RDB$FIELD_LENGTH / CH.RDB$BYTES_PER_CHARACTER)) || ')'
		    WHEN 40 THEN 'CSTRING' || (TRUNC(F.RDB$FIELD_LENGTH / CH.RDB$BYTES_PER_CHARACTER)) || ')'
		    WHEN 45 THEN 'BLOB_ID'
		    WHEN 261 THEN 'BLOB SUB_TYPE ' || F.RDB$FIELD_SUB_TYPE
		    ELSE 'RDB$FIELD_TYPE: ' || F.RDB$FIELD_TYPE || '?'
		  END FIELD_TYPE,
		  IIF(COALESCE(RF.RDB$NULL_FLAG, 0) = 0, NULL, 'NOT NULL') FIELD_NULL,
		  CH.RDB$CHARACTER_SET_NAME FIELD_CHARSET,
		  DCO.RDB$COLLATION_NAME FIELD_COLLATION,
		  COALESCE(RF.RDB$DEFAULT_SOURCE, F.RDB$DEFAULT_SOURCE) FIELD_DEFAULT,
		  F.RDB$VALIDATION_SOURCE FIELD_CHECK,
		  RF.RDB$DESCRIPTION FIELD_DESCRIPTION
		FROM RDB$RELATION_FIELDS RF
		JOIN RDB$FIELDS F ON (F.RDB$FIELD_NAME = RF.RDB$FIELD_SOURCE)
		LEFT OUTER JOIN RDB$CHARACTER_SETS CH ON (CH.RDB$CHARACTER_SET_ID = F.RDB$CHARACTER_SET_ID)
		LEFT OUTER JOIN RDB$COLLATIONS DCO ON ((DCO.RDB$COLLATION_ID = F.RDB$COLLATION_ID) AND (DCO.RDB$CHARACTER_SET_ID = F.RDB$CHARACTER_SET_ID))
		WHERE (RF.RDB$RELATION_NAME = '" + tableName + @"') AND (COALESCE(RF.RDB$SYSTEM_FLAG, 0) = 0)
		ORDER BY RF.RDB$FIELD_POSITION;
";
            string field_name = string.Empty;
            string field_type = string.Empty;
            string field_null = string.Empty;
            string field_charset = string.Empty;
            string field_collation = string.Empty;
            string field_default = string.Empty;
            string field_check = string.Empty;
            string field_description = string.Empty;

            sb.AppendLine("FIELD_NAME,FIELD_TYPE,FIELD_NULL,FIELD_CHARSET,FIELD_COLLATION,FIELD_DEFAULT,FIELD_CHECK,FIELD_DESCRIPTION");
            using (fbConnection = new FbConnection(connectionString))
            {
                using (fbCommand = new FbCommand(sql, fbConnection))
                {
                    fbConnection.Open();
                    using (fbDataReader = fbCommand.ExecuteReader())
                    {
                        while (fbDataReader.Read())
                        {
                            field_name = fbDataReader["FIELD_NAME"].ToString();
                            field_type = fbDataReader["FIELD_TYPE"].ToString();
                            field_null = fbDataReader["FIELD_NULL"].ToString();
                            field_charset = fbDataReader["FIELD_CHARSET"].ToString();
                            field_collation = fbDataReader["FIELD_COLLATION"].ToString();
                            field_default = fbDataReader["FIELD_DEFAULT"].ToString();
                            field_check = fbDataReader["FIELD_CHECK"].ToString();
                            field_description = fbDataReader["FIELD_DESCRIPTION"].ToString();
                            sb.AppendLine($"{field_name.Trim()},{field_type.Trim()},{field_null.Trim()},{field_charset.Trim()},{field_collation.Trim()},{field_default.Trim()},{field_check.Trim()},{field_description.Trim()}");
                        }
                    }
                }
            }

            MessageBox.Show("Done");
            SaveText(sb.ToString());
            OpenText();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            tableName = txtTableName.Text.Trim();
            string s = @"
SELECT
    detail_index_segments.rdb$field_name AS field_name,
    master_relation_constraints.rdb$relation_name AS reference_table,
    master_index_segments.rdb$field_name AS fk_field

FROM
    rdb$relation_constraints detail_relation_constraints
    JOIN rdb$index_segments detail_index_segments ON detail_relation_constraints.rdb$index_name = detail_index_segments.rdb$index_name
    JOIN rdb$ref_constraints ON detail_relation_constraints.rdb$constraint_name = rdb$ref_constraints.rdb$constraint_name -- Master indeksas
    JOIN rdb$relation_constraints master_relation_constraints ON rdb$ref_constraints.rdb$const_name_uq = master_relation_constraints.rdb$constraint_name
    JOIN rdb$index_segments master_index_segments ON master_relation_constraints.rdb$index_name = master_index_segments.rdb$index_name

WHERE
    detail_relation_constraints.rdb$constraint_type = 'FOREIGN KEY'
    AND detail_relation_constraints.rdb$relation_name = '" + tableName + @"'
";
            string field_name = string.Empty;
            string reference_table = string.Empty;
            string fk_field = string.Empty;

            sb.AppendLine("MAIN_TABLE,FIELD_NAME,REFERENCE_TABLE,FK_FIELD");
            using (fbConnection = new FbConnection(connectionString))
            {
                using (fbCommand = new FbCommand(s, fbConnection))
                {
                    fbConnection.Open();
                    using (fbDataReader = fbCommand.ExecuteReader())
                    {
                        while (fbDataReader.Read())
                        {
                            field_name = fbDataReader["FIELD_NAME"].ToString();
                            reference_table = fbDataReader["REFERENCE_TABLE"].ToString();
                            fk_field = fbDataReader["FK_FIELD"].ToString();
                            sb.AppendLine($"{tableName},{field_name.Trim()},{reference_table.Trim()},{fk_field.Trim()}");
                        }
                    }
                }
            }

            MessageBox.Show("Done");
            SaveText(sb.ToString());
            OpenText();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string sql = @"
SELECT
  R.RDB$RELATION_NAME,
  R.RDB$FIELD_NAME,
  R.RDB$FIELD_SOURCE,
  F.RDB$FIELD_LENGTH,
  F.RDB$FIELD_TYPE,
  F.RDB$FIELD_SCALE,
  F.RDB$FIELD_SUB_TYPE
FROM
  RDB$RELATION_FIELDS R
  JOIN RDB$FIELDS F
    ON F.RDB$FIELD_NAME = R.RDB$FIELD_SOURCE
  JOIN RDB$RELATIONS RL
    ON RL.RDB$RELATION_NAME = R.RDB$RELATION_NAME
WHERE
  COALESCE(R.RDB$SYSTEM_FLAG, 0) = 0
  AND
  COALESCE(RL.RDB$SYSTEM_FLAG, 0) = 0
  AND
  RL.RDB$VIEW_BLR IS NULL
ORDER BY
  R.RDB$RELATION_NAME,
  R.RDB$FIELD_POSITION
";
        }
    }
}