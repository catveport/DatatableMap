using DataTableMap;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {

            var datatableA = GetDataTableA();
            var datatableB = GetDataTableB();
            var datatableC = GetDataTableC();
            datatableA.TableName = "A";
            datatableB.TableName = "B";
            datatableC.TableName = "C";

            var resul = datatableA.MapToEntity<A>(new DataTable[] { datatableB, datatableC });

        }

        static DataTable GetDataTableA()
        {
            var table = new DataTable();
            table.Columns.Add("AId", typeof(int));
            table.Columns.Add("AIdNull", typeof(int));
            table.Columns.Add("ANumber", typeof(string));
            table.Columns.Add("ADate", typeof(DateTime));
            table.Columns.Add("ADateNull", typeof(DateTime));
            table.Columns.Add("ABytes", typeof(byte[]));
            table.Columns.Add("ABytesNull", typeof(byte[]));
            table.Columns.Add("ADecimal", typeof(decimal));
            table.Columns.Add("ADecimalNull", typeof(decimal));
            table.Columns.Add("ALong", typeof(long));
            table.Columns.Add("ALongNull", typeof(long));
            table.Columns.Add("ABool", typeof(bool));
            table.Columns.Add("ABoolNull", typeof(bool));
            table.Columns.Add("AExtra", typeof(string));

            var bytes = new Byte[] { 0, 0, 1 };

            // Here we add five DataRows.
            table.Rows.Add(1, null, "10", DateTime.Now, null, bytes, null, 457845.45, null, 121512445454, null, 0, null, "extra");
            table.Rows.Add(2, 12, "20", DateTime.Now, DateTime.Now, bytes, null, 457845.45, 458754.5, 121512445454, 12154514247, 1, 0, "extra");
            table.Rows.Add(3, null, "30", DateTime.Now, null, bytes, null, 43437845.45, null, 656445454, null, 1, null, "extra");
            table.Rows.Add(4, 74, "40", DateTime.Now, DateTime.Now, bytes, null, 845.45, 12.2, 45454, 4578548754875, 0, 1, "extra");
            table.Rows.Add(5, null, "50", DateTime.Now, null, bytes, null, 45455.45, null, 989145454, null, 1, null, "extra");
            return table;
        }

        static DataTable GetDataTableB()
        {
            var table = new DataTable();
            table.Columns.Add("BId", typeof(int));
            table.Columns.Add("BIdNull", typeof(int));
            table.Columns.Add("BNumber", typeof(string));
            table.Columns.Add("BDate", typeof(DateTime));
            table.Columns.Add("BDateNull", typeof(DateTime));
            table.Columns.Add("BDecimal", typeof(decimal));
            table.Columns.Add("BDecimalNull", typeof(decimal));
            table.Columns.Add("BLong", typeof(long));
            table.Columns.Add("BLongNull", typeof(long));
            table.Columns.Add("BBool", typeof(bool));
            table.Columns.Add("BBoolNull", typeof(bool));
            table.Columns.Add("AIdFK", typeof(int));
            table.Columns.Add("ANumberFK", typeof(string));
            table.Columns.Add("BExtra", typeof(string));

            // Here we add five DataRows.
            table.Rows.Add(1, null, "10", DateTime.Now, null, 457845.45, null, 121512445454, null, 1, null, 5, 50, "extra");
            table.Rows.Add(2, 12, "20", DateTime.Now, DateTime.Now, 457845.45, 458754.5, 121512445454, 12154514247, 1, 0, 5, 50, "extra");
            table.Rows.Add(3, null, "30", DateTime.Now, null, 43437845.45, null, 656445454, null, 0, null, 3, 30, "extra");
            table.Rows.Add(4, 74, "40", DateTime.Now, DateTime.Now, 845.45, 12.2, 45454, 4578548754875, 1, 1, 2, 20, "extra");
            table.Rows.Add(5, null, "50", DateTime.Now, null, 45455.45, null, 989145454, null, 0, null, 2, 20, "extra");
            return table;
        }

        static DataTable GetDataTableC()
        {
            var table = new DataTable();
            table.Columns.Add("CId", typeof(int));
            table.Columns.Add("CBool", typeof(bool));
            table.Columns.Add("AIdFK", typeof(int));
            table.Columns.Add("BIdFK", typeof(int));

            // Here we add five DataRows.
            table.Rows.Add(1, 1, 5, 4);
            table.Rows.Add(2, 0, 4, 3);
            table.Rows.Add(3, 1, 3, 2);
            table.Rows.Add(4, 1, 2);
            table.Rows.Add(5, 0, 1);
            return table;
        }

    }

    public class A
    {
        [ColumnName("AId")]
        public int AIdentenfication { get; set; }

        [ColumnName("AIdNull")]
        public int? AltIdentenfication { get; set; }

        [ColumnName("ANumber")]
        public string Number { get; set; }

        [ColumnName("ADate")]
        public DateTime Date { get; set; }

        [ColumnName("ADateNull")]
        public DateTime? AtlDate { get; set; }

        [ColumnName("ADecimal")]
        public decimal Cost { get; set; }

        [ColumnName("ADecimalNull")]
        public decimal? AltCost { get; set; }

        [ColumnName("ABytes")]
        public byte[] Data { get; set; }

        [ColumnName("ABytesNull")]
        public byte[] Data2 { get; set; }

        [ColumnName("ALong")]
        public long Value { get; set; }

        [ColumnName("ALongNull")]
        public long? AltValue { get; set; }

        [ColumnName("ABool")]
        public bool IsGood { get; set; }

        [ColumnName("ABoolNull")]
        public bool? AtlIsGood { get; set; }

        [OneToManyRelation("B", new string[] { "AId", "ANumber" }, new string[] { "AIdFK", "ANumberFK" })]
        public List<B> children { get; set; }

        [OneToOneRelation("C", new string[] { "AId", "ABool" }, new string[] { "AIdFK", "CBool" })]
        public C Sib { get; set; }

        public string AExtra { get; set; }

    }

    public class B
    {
        [ColumnName("BId")]
        public int BIdentenfication { get; set; }

        [ColumnName("BIdNull")]
        public int? AltIdentenfication { get; set; }

        [ColumnName("BNumber")]
        public string Number { get; set; }

        [ColumnName("BDate")]
        public DateTime Date { get; set; }

        [ColumnName("BDateNull")]
        public DateTime? AtlDate { get; set; }

        [ColumnName("BDecimal")]
        public decimal Cost { get; set; }

        [ColumnName("BDecimalNull")]
        public decimal? AltCost { get; set; }
        
        [ColumnName("BLong")]
        public long Value { get; set; }

        [ColumnName("BLongNull")]
        public long? AltValue { get; set; }

        [ColumnName("BBool")]
        public bool IsGood { get; set; }

        [ColumnName("BBoolNull")]
        public bool? AtlIsGood { get; set; }

        [OneToOneRelation("C", "BId", "BIdFK")]
        public C Sib { get; set; }

        [NotMapped]
        public string Extra2 { get; set; }

        public string BExtra { get; set; }
        
        [ColumnName("AIdFK")]
        public int AIdentenfication { get; set; }

        [ParentNavegation]
        public A ANav { get; set; }

    }

    public class C
    {
        [ColumnName("CId")]
        public int CIdentenfication { get; set; }

        //[ColumnName("CBool")]
        public bool CBool { get; set; }

        [NotMapped]
        public string Extra { get; set; }

        [ColumnName("AIdFK")]
        public int AIdentenfication { get; set; }

        [ColumnName("BIdFK")]
        public int BIdentenfication { get; set; }

        [ParentNavegation]
        public A ANav { get; set; }

        [ParentNavegation]
        public B BNav { get; set; }
        
    }
}
