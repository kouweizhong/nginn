using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text;
using XmlForms.Interfaces;
using Spring.Core;
using NLog;
using Sooda;
using Sooda.QL;
using Sooda.Sql;
using System.IO;
using Sooda.Schema;
using System.Data;
using System.Collections;
using System.Xml;

namespace XmlForms.Lists
{
    class SoodaListDataProvider : IListDataProvider
    {
        private IListInfoProvider _infoProvider;
        private Sooda.Schema.SchemaInfo _dbSchema;
        private static Logger log = LogManager.GetCurrentClassLogger();

        public IListInfoProvider ListInfoProvider
        {
            get { return _infoProvider; }
            set { _infoProvider = value; }
        }

        public Sooda.Schema.SchemaInfo DatabaseSchema
        {
            get { return _dbSchema; }
            set { _dbSchema = value; }
        }

       



        public string GetListData(string listName, string listQuery)
        {
            ListInfo li = ListInfoProvider.GetListInfo(listName);

            StringWriter sw = new StringWriter();
            GetListData(li, listQuery, sw);

            return sw.ToString();
        }

        protected virtual void GetListData(ListInfo li, string listQuery, TextWriter output)
        {
            SoqlBooleanExpression expr = new SoqlBooleanLiteralExpression(true);
            SoqlQueryExpression exp = BuildDataQuery(li, expr, new List<string>(), 0, 1000, "", new List<string>());

            ArrayList qParams = new ArrayList();
            XmlWriterSettings s = new XmlWriterSettings();
            s.OmitXmlDeclaration = true;
            XmlWriter xw = XmlWriter.Create(output, s);
            xw.WriteStartElement("results");
            using (SoodaDataSource ds = DatabaseSchema.DataSources[0].CreateDataSource())
            {
                ds.Open();
                string sql = SoqlToSql(exp, (SqlDataSource) ds);

                using (IDataReader dr = ds.ExecuteRawQuery(sql, qParams))
                {
                    while(dr.Read())
                    {
                        xw.WriteStartElement("row");
                        for (int i = 0; i < dr.FieldCount; i++)
                        {
                            xw.WriteElementString(dr.GetName(i), Convert.ToString(dr.GetValue(i)));
                        }
                        xw.WriteEndElement();
                    }
                }
            }
            xw.WriteEndElement();
            xw.Flush();
        }

        protected string SoqlToSql(SoqlQueryExpression expr, SqlDataSource ds)
        {
            using (StringWriter sw1 = new StringWriter())
            {
                SoqlToSqlConverter converter = new SoqlToSqlConverter(sw1, this.DatabaseSchema, ds.SqlBuilder);
                log.Trace("Converting: {0}", expr);
                converter.ConvertQuery(expr);
                log.Trace("Converted: {0}", sw1);
                return sw1.ToString();
            }
        }

        protected void GetQueryResultsXml(ListInfo li, string query, TextWriter output)
        {
            
        }


        protected virtual SoqlQueryExpression BuildDataQuery(ListInfo li, SoqlBooleanExpression whereClause, IList<string> theQueryParameters, int startIndex, int topCount, string orderBy, IList<string> selectAliases)
        {
            SoqlQueryExpression theQuery;

            theQuery = new SoqlQueryExpression();
            theQuery.SelectExpressions = new SoqlExpressionCollection();
            theQuery.SelectAliases = new StringCollection();
            theQuery.TopCount = topCount;

            theQuery.From.Add(li.RecordClass);
            theQuery.FromAliases.Add(String.Empty);

            if (orderBy != null)
            {
                //ParseOrderBy(theQuery, orderBy, columns, theQueryParameters);
            }

            // special columns - class Name and key and label


            theQuery.SelectExpressions.Add(new SoqlSoodaClassExpression());
            theQuery.SelectAliases.Add(String.Empty);

            ClassInfo ci = DatabaseSchema.FindClassByName(li.RecordClass);
            Sooda.Schema.FieldInfo key_fi = ci.GetPrimaryKeyFields()[0]; // ci.GetPrimaryKeyField();

            //theQuery.SelectExpressions.Add(new SoqlPathExpression(key_fi.Name));
            //theQuery.SelectAliases.Add(String.Empty);

            foreach (ListColumnInfo lci in li.Columns)
            {
                string alias = lci.DataField;
                alias = alias.Replace('.', '_');
                if (lci.DataExpression != null)
                {
                    theQuery.SelectExpressions.Add(SoqlParser.ParseExpression(lci.DataExpression));
                }
                else
                {
                    theQuery.SelectExpressions.Add(SoqlParser.ParseExpression(lci.DataField));
                }
                theQuery.SelectAliases.Add(alias);
                //selectAliases.Add(alias);
            }

            theQuery.WhereClause = whereClause;

            return theQuery;
        }

    }
}
