using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;

namespace PROYECTO_RESIDENCIAS
{
    public static class AuxRepo
    {
        public class MesaDto
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public int? Capacidad { get; set; }
            public string Estado { get; set; }
        }

        public class MeseroDto
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public bool Activo { get; set; }
        }

        

        // ===== MESAS =====
        public static List<MesaDto> ListarMesas()
        {
            var list = new List<MesaDto>();
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "UTF8");
            using var cmd = new FbCommand(@"SELECT ID_MESA, NOMBRE, CAPACIDAD, ESTADO FROM MESAS ORDER BY ID_MESA", conn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new MesaDto
                {
                    Id = Convert.ToInt32(rd["ID_MESA"]),
                    Nombre = rd["NOMBRE"].ToString(),
                    Capacidad = rd["CAPACIDAD"] == DBNull.Value ? (int?)null : Convert.ToInt32(rd["CAPACIDAD"]),
                    Estado = rd["ESTADO"]?.ToString() ?? "LIBRE"
                });
            }
            return list;
        }

        public static int InsertMesa(string nombre, int? capacidad)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Nombre requerido");
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "UTF8");
            using var tx = conn.BeginTransaction();
            using var cmd = new FbCommand(@"INSERT INTO MESAS (NOMBRE, CAPACIDAD, ESTADO) VALUES (@N,@C,'LIBRE') RETURNING ID_MESA", conn, tx);
            cmd.Parameters.Add(new FbParameter("@N", FbDbType.VarChar, 30) { Value = nombre.Trim() });
            cmd.Parameters.Add(new FbParameter("@C", FbDbType.Integer) { Value = (object?)capacidad ?? DBNull.Value });
            int id = Convert.ToInt32(cmd.ExecuteScalar());
            tx.Commit();
            return id;
        }

        public static void UpdateMesa(int id, string nombre, int? capacidad)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "UTF8");
            using var cmd = new FbCommand(@"UPDATE MESAS SET NOMBRE=@N, CAPACIDAD=@C WHERE ID_MESA=@ID", conn);
            cmd.Parameters.Add(new FbParameter("@N", FbDbType.VarChar, 30) { Value = nombre?.Trim() ?? "" });
            cmd.Parameters.Add(new FbParameter("@C", FbDbType.Integer) { Value = (object?)capacidad ?? DBNull.Value });
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = id });
            cmd.ExecuteNonQuery();
        }

        public static void DeleteMesa(int id)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "UTF8");
            using var cmd = new FbCommand(@"DELETE FROM MESAS WHERE ID_MESA=@ID", conn);
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = id });
            cmd.ExecuteNonQuery();
        }

        // ===== MESEROS =====
        public static List<MeseroDto> ListarMeseros(bool soloActivos = true)
        {
            var list = new List<MeseroDto>();
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "UTF8");
            string sql = "SELECT ID_MESERO, NOMBRE, ACTIVO FROM MESEROS " + (soloActivos ? "WHERE ACTIVO=1 " : "") + "ORDER BY NOMBRE";
            using var cmd = new FbCommand(sql, conn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new MeseroDto
                {
                    Id = Convert.ToInt32(rd["ID_MESERO"]),
                    Nombre = rd["NOMBRE"].ToString(),
                    Activo = Convert.ToInt16(rd["ACTIVO"]) == 1
                });
            }
            return list;
        }

        public static int InsertMesero(string nombre, bool activo)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Nombre requerido");
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "UTF8");
            using var tx = conn.BeginTransaction();
            using var cmd = new FbCommand(@"INSERT INTO MESEROS (NOMBRE, ACTIVO) VALUES (@N, @A) RETURNING ID_MESERO", conn, tx);
            cmd.Parameters.Add(new FbParameter("@N", FbDbType.VarChar, 60) { Value = nombre.Trim() });
            cmd.Parameters.Add(new FbParameter("@A", FbDbType.SmallInt) { Value = activo ? 1 : 0 });
            int id = Convert.ToInt32(cmd.ExecuteScalar());
            tx.Commit();
            return id;
        }

        public static void UpdateMesero(int id, string nombre, bool activo)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "UTF8");
            using var cmd = new FbCommand(@"UPDATE MESEROS SET NOMBRE=@N, ACTIVO=@A WHERE ID_MESERO=@ID", conn);
            cmd.Parameters.Add(new FbParameter("@N", FbDbType.VarChar, 60) { Value = nombre?.Trim() ?? "" });
            cmd.Parameters.Add(new FbParameter("@A", FbDbType.SmallInt) { Value = activo ? 1 : 0 });
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = id });
            cmd.ExecuteNonQuery();
        }

        public static void DeleteMesero(int id)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "UTF8");
            using var cmd = new FbCommand(@"DELETE FROM MESEROS WHERE ID_MESERO=@ID", conn);
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = id });
            cmd.ExecuteNonQuery();
        }
    }
}
