using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Npgsql;
using Elasticsearch;
using Nest;


namespace Strabo.Core.TextRecognition
{
    public class GeoDictionary
    {
        public static List<String> generic_dictionary = new List<string>();
        public static List<string> geo_dictionary = new List<String>();

        public List<string> ReadDictionary(double top, double left, double bottom, double right, bool dictProcessed)
        {

            
            

            // connect to db
            // do a spatial query tp find the word
            string database="dictionary";
            string user="postgres";
            string password = "@ahf143!!";
            string server = "localhost";
            string port = "5432";
            string connstring = String.Format("Server={0};Port={1};" +
                    "User Id={2};Password={3};Database={4};CommandTimeout=6000",
                    server, port, user,
                    password, database);
            NpgsqlConnection conn = new NpgsqlConnection(connstring);
            conn.Open();

            string sql = "SELECT name FROM general_terms";
            // Define a query
            NpgsqlCommand command;
            NpgsqlDataReader dr;
            HashSet<string> temp = new HashSet<string>() ;

            if (dictProcessed)
            {
                command = new NpgsqlCommand(sql, conn);

                // Execute the query and obtain a result set
                dr = command.ExecuteReader();



                while (dr.Read())
                {
                    //Console.Write("{0}\t{1}\t{2} \n", dr[0], dr[1], dr[2]);
                    foreach (var item in dr[0].ToString().Trim().Split(new Char[] { ',', ' ', '&', '-', ':', '[', ']', '/', '(', ')', '.' }))
                    {
                        temp.Add(item);
                    }
                }
                generic_dictionary = temp.ToList();
            }

            HashSet<string> id = new HashSet<string>();
            temp.Clear();
            Console.WriteLine("BEGIN QUESRY");

            sql = "SELECT id FROM location WHERE location.geom && ST_MakeEnvelope("+left+", "+top+", "+right+", "+bottom+", 4326)";
            command = new NpgsqlCommand(sql, conn);
            dr = command.ExecuteReader();

            while(dr.Read())
            {
                id.Add(dr[0].ToString());
            }

            foreach(var item in id)
            {
                sql = "SELECT name FROM entity WHERE id = " + item;
                command = new NpgsqlCommand(sql, conn);
                dr = command.ExecuteReader();
                while(dr.Read())
                {
                    foreach (var item2 in dr[0].ToString().Trim().Split(new Char[] { ',', '?', ' ', '&', '-', ':', '[', ']', '/', '(', ')', '.' }))
                    {
                        temp.Add(item2);
                    }
                }
            }

            geo_dictionary = temp.ToList();

            conn.Close();
            return null;

        }
    }
}
