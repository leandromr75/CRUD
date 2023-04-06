using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    public static class DAL
    {
        public static DataTable Listar()
        {
            //strConnection = "Provider=sqloledb;Network Library=DBMSSOCN;Data Source=" + ip + "," + porta + ";Initial Catalog=AMBRA;User ID=sa;Password=#lecoteco1975";
            
            String strConnection = "Data Source=.\\SQLEXPRESS;Initial Catalog=SUCOS_VENDAS;User ID=teste;Password=123 ;Provider=SQLOLEDB";
            String strSQL = "select * from [TABELA DE PRODUTOS]";
            //cria a conexão com o banco de dados
            OleDbConnection dbConnection = new OleDbConnection(strConnection);
            //cria a conexão com o banco de dados
            OleDbConnection con = new OleDbConnection(strConnection);
            //cria o objeto command para executar a instruçao sql
            OleDbCommand cmd = new OleDbCommand(strSQL, con);
            //abre a conexao
            con.Open();
            cmd.CommandType = CommandType.Text;
            //cmd.Parameters.AddWithValue("@CFOP_Codigo", cfop_codigo);

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            //cria um objeto datatable
            DataTable clientes = new DataTable();
            //preenche o datatable via dataadapter
            da.Fill(clientes);
            con.Dispose();
            con.Close();
            cmd.Dispose();
            dbConnection.Dispose();
            dbConnection.Close();
            //atribui o datatable ao datagridview para exibir o resultado
            //dataGridView1.DataSource = clientes;
            return clientes;
        }

        public static void Inserir()
        {
            String strConnection = "Data Source=.\\SQLEXPRESS;Initial Catalog=SUCOS_VENDAS;User ID=teste;Password=123 ;Provider=SQLOLEDB";
            
            //cria a conexão com o banco de dados
            OleDbConnection con = new OleDbConnection(strConnection);
            //cria o objeto command para executar a instruçao sql
            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "insert into [TABELA DE PRODUTOS] ([CODIGO DO PRODUTO],[NOME DO PRODUTO],EMBALAGEM,TAMANHO,SABOR, [PREÇO DE LISTA]) " +
                " values(?,?,?,?,?,?)";
            cmd.Parameters.AddWithValue("@CODIGO", 12345678);
            cmd.Parameters.AddWithValue("@NOME", "teste");
            cmd.Parameters.AddWithValue("@EMBALAGEM", "TESTE");
            cmd.Parameters.AddWithValue("@TAMANHO", "teste");
            cmd.Parameters.AddWithValue("@SABOR", "TESTE");
            cmd.Parameters.AddWithValue("@PREÇO", 6.00);
            cmd.Connection = con;
            con.Open();
            cmd.ExecuteNonQuery();




            con.Dispose();
            con.Close();
            cmd.Dispose();
            
            //atribui o datatable ao datagridview para exibir o resultado
            //dataGridView1.DataSource = clientes;

        }

        public static void Deletar(string codigo)
        {
            String strConnection = "Data Source=.\\SQLEXPRESS;Initial Catalog=SUCOS_VENDAS;User ID=teste;Password=123 ;Provider=SQLOLEDB";

            //cria a conexão com o banco de dados
            OleDbConnection con = new OleDbConnection(strConnection);
            //cria o objeto command para executar a instruçao sql
            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "delete [TABELA DE PRODUTOS] where[CODIGO DO PRODUTO] = " + codigo;
            
            cmd.Connection = con;
            con.Open();
            cmd.ExecuteNonQuery();


            con.Dispose();
            con.Close();
            cmd.Dispose();

            //atribui o datatable ao datagridview para exibir o resultado
            //dataGridView1.DataSource = clientes;

        }

        public static void Atualizar(string codigo, string nome)
        {
            String strConnection = "Data Source=.\\SQLEXPRESS;Initial Catalog=SUCOS_VENDAS;User ID=teste;Password=123 ;Provider=SQLOLEDB";

            //cria a conexão com o banco de dados
            OleDbConnection con = new OleDbConnection(strConnection);
            //cria o objeto command para executar a instruçao sql
            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "update [TABELA DE PRODUTOS] set [NOME DO PRODUTO] = '" + nome + "' where [CODIGO DO PRODUTO] = " + codigo;

            cmd.Connection = con;
            con.Open();
            cmd.ExecuteNonQuery();


            con.Dispose();
            con.Close();
            cmd.Dispose();

            //atribui o datatable ao datagridview para exibir o resultado
            //dataGridView1.DataSource = clientes;

        }

        public static DataTable Localizar(string consulta)
        {
            //strConnection = "Provider=sqloledb;Network Library=DBMSSOCN;Data Source=" + ip + "," + porta + ";Initial Catalog=AMBRA;User ID=sa;Password=#lecoteco1975";

            String strConnection = "Data Source=.\\SQLEXPRESS;Initial Catalog=SUCOS_VENDAS;User ID=teste;Password=123 ;Provider=SQLOLEDB";
            String strSQL = "select * from [TABELA DE PRODUTOS] where [NOME DO PRODUTO] like '%" + consulta + "%'";
            //cria a conexão com o banco de dados
            OleDbConnection dbConnection = new OleDbConnection(strConnection);
            //cria a conexão com o banco de dados
            OleDbConnection con = new OleDbConnection(strConnection);
            //cria o objeto command para executar a instruçao sql
            OleDbCommand cmd = new OleDbCommand(strSQL, con);
            //abre a conexao
            con.Open();
            cmd.CommandType = CommandType.Text;
            //cmd.Parameters.AddWithValue("@CFOP_Codigo", cfop_codigo);

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            //cria um objeto datatable
            DataTable clientes = new DataTable();
            //preenche o datatable via dataadapter
            da.Fill(clientes);
            con.Dispose();
            con.Close();
            cmd.Dispose();
            dbConnection.Dispose();
            dbConnection.Close();
            //atribui o datatable ao datagridview para exibir o resultado
            //dataGridView1.DataSource = clientes;
            return clientes;
        }
    }
}
