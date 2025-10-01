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
            // nuevos:
            public int? MeseroIdActual { get; set; }
            public string MeseroNombre { get; set; }
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
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");

            int? idTurno = GetTurnoAbiertoId();

            string sql = idTurno.HasValue
                ? @"SELECT me.ID_MESA, me.NOMBRE, me.CAPACIDAD, me.ESTADO,
                    mt.ID_MESERO, ms.NOMBRE
           FROM MESAS me
           LEFT JOIN MESA_TURNO mt ON mt.ID_MESA = me.ID_MESA
                                  AND mt.ID_TURNO = @T
                                  AND (mt.ESTADO IN ('OCUPADA','EN_CUENTA'))
           LEFT JOIN MESEROS ms ON ms.ID_MESERO = mt.ID_MESERO
           ORDER BY me.ID_MESA"
                : @"SELECT me.ID_MESA, me.NOMBRE, me.CAPACIDAD, me.ESTADO,
                    CAST(NULL AS INTEGER) AS ID_MESERO,
                    CAST(NULL AS VARCHAR(60)) AS NOMBRE
           FROM MESAS me
           ORDER BY me.ID_MESA";

            using var cmd = new FirebirdSql.Data.FirebirdClient.FbCommand(sql, conn);
            if (idTurno.HasValue)
                cmd.Parameters.Add("@T", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idTurno.Value;

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new MesaDto
                {
                    Id = Convert.ToInt32(rd[0]),
                    Nombre = rd[1]?.ToString(),
                    Capacidad = rd[2] == DBNull.Value ? (int?)null : Convert.ToInt32(rd[2]),
                    Estado = rd[3]?.ToString(),
                    MeseroIdActual = rd[4] == DBNull.Value ? (int?)null : Convert.ToInt32(rd[4]),
                    MeseroNombre = rd[5] == DBNull.Value ? null : rd[5].ToString()
                });
            }

            return list;
        }


        public static int InsertMesa(string nombre, int? capacidad)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Nombre requerido");
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
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
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand(@"UPDATE MESAS SET NOMBRE=@N, CAPACIDAD=@C WHERE ID_MESA=@ID", conn);
            cmd.Parameters.Add(new FbParameter("@N", FbDbType.VarChar, 30) { Value = nombre?.Trim() ?? "" });
            cmd.Parameters.Add(new FbParameter("@C", FbDbType.Integer) { Value = (object?)capacidad ?? DBNull.Value });
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = id });
            cmd.ExecuteNonQuery();
        }

        public static void DeleteMesa(int id)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand(@"DELETE FROM MESAS WHERE ID_MESA=@ID", conn);
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = id });
            cmd.ExecuteNonQuery();
        }

        // ===== MESEROS =====
        public static List<MeseroDto> ListarMeseros(bool soloActivos = true)
        {
            var list = new List<MeseroDto>();
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
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
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
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
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand(@"UPDATE MESEROS SET NOMBRE=@N, ACTIVO=@A WHERE ID_MESERO=@ID", conn);
            cmd.Parameters.Add(new FbParameter("@N", FbDbType.VarChar, 60) { Value = nombre?.Trim() ?? "" });
            cmd.Parameters.Add(new FbParameter("@A", FbDbType.SmallInt) { Value = activo ? 1 : 0 });
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = id });
            cmd.ExecuteNonQuery();
        }

        public static void DeleteMesero(int id)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand(@"DELETE FROM MESEROS WHERE ID_MESERO=@ID", conn);
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = id });
            cmd.ExecuteNonQuery();
        }

        public static int GetOrOpenTurnoDelDia()
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var tx = conn.BeginTransaction();

            // Turno abierto de HOY = FECHA=CURRENT_DATE y HORA_FIN IS NULL
            int? id = null;
            using (var cmd = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "SELECT ID_TURNO FROM TURNOS WHERE FECHA = CURRENT_DATE AND HORA_FIN IS NULL ROWS 1",
                conn, tx))
            {
                var o = cmd.ExecuteScalar();
                if (o != null && o != DBNull.Value) id = System.Convert.ToInt32(o);
            }

            if (id.HasValue)
            {
                tx.Commit();
                return id.Value;
            }

            // Crear turno del día (HORA_INI requerida; FECHA la rellena el trigger si no la pasas)
            using (var cmdIns = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "INSERT INTO TURNOS (FECHA, HORA_INI, RESPONSABLE) " +
                "VALUES (CURRENT_DATE, CURRENT_TIME, 'SISTEMA') RETURNING ID_TURNO",
                conn, tx))
            {
                int nuevo = System.Convert.ToInt32(cmdIns.ExecuteScalar());
                tx.Commit();
                return nuevo;
            }
        }



        public sealed class AbrirMesaResult
        {
            public int IdTurno { get; set; }
            public int IdMesaTurno { get; set; }
            public int IdPedido { get; set; }
        }

        // Lanza exception si la mesa no está libre o algo falla.
        public static AbrirMesaResult AbrirMesa(int idMesa, int idMesero)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var tx = conn.BeginTransaction();

            // 1) Turno del día (abierto)
            int idTurno;
            using (var cmdT = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "SELECT ID_TURNO FROM TURNOS WHERE FECHA = CURRENT_DATE AND HORA_FIN IS NULL ROWS 1",
                conn, tx))
            {
                var o = cmdT.ExecuteScalar();
                if (o == null || o == DBNull.Value)
                {
                    using var cmdIns = new FirebirdSql.Data.FirebirdClient.FbCommand(
                        "INSERT INTO TURNOS (FECHA, HORA_INI, RESPONSABLE) " +
                        "VALUES (CURRENT_DATE, CURRENT_TIME, 'SISTEMA') RETURNING ID_TURNO",
                        conn, tx);
                    idTurno = System.Convert.ToInt32(cmdIns.ExecuteScalar());
                }
                else idTurno = System.Convert.ToInt32(o);
            }

            // 2) Validar mesa LIBRE
            using (var cmdMesa = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "SELECT ESTADO FROM MESAS WHERE ID_MESA=@M", conn, tx))
            {
                cmdMesa.Parameters.Add("@M", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idMesa;
                var estado = cmdMesa.ExecuteScalar()?.ToString();
                if (!string.Equals(estado, "LIBRE", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("La mesa no está LIBRE.");
            }

            // 3) Checar que no exista mesa-turno abierta (OCUPADA/EN_CUENTA) para este turno
            using (var cmdChk = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "SELECT ID_MESA_TURNO FROM MESA_TURNO " +
                "WHERE ID_TURNO=@T AND ID_MESA=@M AND (ESTADO IN ('OCUPADA','EN_CUENTA')) ROWS 1",
                conn, tx))
            {
                cmdChk.Parameters.Add("@T", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idTurno;
                cmdChk.Parameters.Add("@M", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idMesa;
                var exists = cmdChk.ExecuteScalar();
                if (exists != null && exists != DBNull.Value)
                    throw new InvalidOperationException("La mesa ya tiene un registro abierto en este turno.");
            }

            // 4) Crear MESA_TURNO (no hay campos de hora en tu esquema)
            int idMesaTurno;
            using (var cmdMT = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "INSERT INTO MESA_TURNO (ID_TURNO, ID_MESA, ID_MESERO, ESTADO) " +
                "VALUES (@T, @M, @ME, 'OCUPADA') RETURNING ID_MESA_TURNO",
                conn, tx))
            {
                cmdMT.Parameters.Add("@T", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idTurno;
                cmdMT.Parameters.Add("@M", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idMesa;
                cmdMT.Parameters.Add("@ME", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idMesero;
                idMesaTurno = System.Convert.ToInt32(cmdMT.ExecuteScalar());
            }

            // 5) Crear PEDIDO (trigger pone FECHA_HORA y ESTADO='ABIERTO')
            int idPedido;
            using (var cmdP = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "INSERT INTO PEDIDOS (ID_MESA_TURNO, SUBTOTAL, IMPUESTO, TOTAL) " +
                "VALUES (@MT, 0, 0, 0) RETURNING ID_PEDIDO",
                conn, tx))
            {
                cmdP.Parameters.Add("@MT", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idMesaTurno;
                idPedido = System.Convert.ToInt32(cmdP.ExecuteScalar());
            }

            // 6) Poner mesa en OCUPADA
            using (var cmdUpd = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "UPDATE MESAS SET ESTADO='OCUPADA' WHERE ID_MESA=@M", conn, tx))
            {
                cmdUpd.Parameters.Add("@M", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idMesa;
                cmdUpd.ExecuteNonQuery();
            }

            tx.Commit();

            return new AbrirMesaResult
            {
                IdTurno = idTurno,
                IdMesaTurno = idMesaTurno,
                IdPedido = idPedido
            };
        }


        // AuxRepo.cs (dentro de la clase AuxRepo)
        private static bool ColumnExists(FirebirdSql.Data.FirebirdClient.FbConnection con,
                                         string table,
                                         string column,
                                         FirebirdSql.Data.FirebirdClient.FbTransaction tx = null)
        {
            var sql = @"
        SELECT COUNT(*)
        FROM RDB$RELATION_FIELDS
        WHERE RDB$RELATION_NAME = @t
          AND RDB$FIELD_NAME    = @c";

            using var cmd = tx != null
                ? new FirebirdSql.Data.FirebirdClient.FbCommand(sql, con, tx)
                : new FirebirdSql.Data.FirebirdClient.FbCommand(sql, con);

            cmd.Parameters.Add("@t", FirebirdSql.Data.FirebirdClient.FbDbType.Char).Value = table.ToUpperInvariant();
            cmd.Parameters.Add("@c", FirebirdSql.Data.FirebirdClient.FbDbType.Char).Value = column.ToUpperInvariant();

            return System.Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public static int? GetTurnoAbiertoId()
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "SELECT ID_TURNO FROM TURNOS WHERE FECHA = CURRENT_DATE AND HORA_FIN IS NULL ROWS 1", conn);
            var o = cmd.ExecuteScalar();
            return (o != null && o != DBNull.Value) ? Convert.ToInt32(o) : (int?)null;
        }


        public class TurnoInfo
        {
            public int Id { get; set; }
            public DateTime Fecha { get; set; }
            public TimeSpan HoraIni { get; set; }
            public TimeSpan? HoraFin { get; set; }
            public string Responsable { get; set; }
            public string Obs { get; set; }
        }

        
        public static TurnoInfo? ObtenerTurnoAbiertoInfo()
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand(@"
        SELECT ID_TURNO, FECHA, HORA_INI, HORA_FIN, RESPONSABLE, COALESCE(OBS,'')
        FROM TURNOS
        WHERE FECHA = CURRENT_DATE AND HORA_FIN IS NULL
        ROWS 1", conn);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;
            return new TurnoInfo
            {
                Id = Convert.ToInt32(rd[0]),
                Fecha = (DateTime)rd[1],
                HoraIni = (TimeSpan)rd[2],
                HoraFin = rd[3] == DBNull.Value ? (TimeSpan?)null : (TimeSpan)rd[3],
                Responsable = rd[4]?.ToString() ?? "",
                Obs = rd[5]?.ToString() ?? ""
            };
        }

        public static int AbrirTurno(string responsable, string obs)
        {
            if (string.IsNullOrWhiteSpace(responsable))
                throw new ArgumentException("Captura el responsable del turno.");

            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var tx = conn.BeginTransaction();

            // si ya hay turno abierto hoy, devuélvelo
            using (var sel = new FbCommand(
                "SELECT ID_TURNO FROM TURNOS WHERE FECHA = CURRENT_DATE AND HORA_FIN IS NULL ROWS 1", conn, tx))
            {
                var o = sel.ExecuteScalar();
                if (o != null && o != DBNull.Value)
                {
                    tx.Commit();
                    return Convert.ToInt32(o);
                }
            }

            using (var ins = new FbCommand(
                "INSERT INTO TURNOS (FECHA, HORA_INI, RESPONSABLE, OBS) " +
                "VALUES (CURRENT_DATE, CURRENT_TIME, @R, @O) RETURNING ID_TURNO", conn, tx))
            {
                ins.Parameters.Add("@R", FbDbType.VarChar, 60).Value = responsable.Trim();
                ins.Parameters.Add("@O", FbDbType.VarChar, 255).Value = (object?)obs ?? DBNull.Value;
                int id = Convert.ToInt32(ins.ExecuteScalar());
                tx.Commit();
                return id;
            }
        }

        public static bool HayMesasAbiertas(int idTurno)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand(
                "SELECT COUNT(*) FROM MESA_TURNO WHERE ID_TURNO=@T AND ESTADO IN ('OCUPADA','EN_CUENTA')", conn);
            cmd.Parameters.Add("@T", FbDbType.Integer).Value = idTurno;
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public static void CerrarTurno(int idTurno)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "UTF8");
            using var tx = conn.BeginTransaction();

            // 1) ¿Mesas con MESA_TURNO abierto en este turno?
            using (var chk = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "SELECT COUNT(*) FROM MESA_TURNO WHERE ID_TURNO=@T AND ESTADO IN ('OCUPADA','EN_CUENTA')",
                conn, tx))
            {
                chk.Parameters.Add("@T", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idTurno;
                int abiertas = System.Convert.ToInt32(chk.ExecuteScalar());
                if (abiertas > 0)
                    throw new InvalidOperationException("No puedes cerrar el turno: hay mesas OCUPADAS/EN_CUENTA en MESA_TURNO.");
            }

            // 2) ¿Mesas marcadas como OCUPADA/EN_CUENTA en MESAS?
            using (var chk2 = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "SELECT COUNT(*) FROM MESAS WHERE ESTADO IN ('OCUPADA','EN_CUENTA')", conn, tx))
            {
                int cnt = System.Convert.ToInt32(chk2.ExecuteScalar());
                if (cnt > 0)
                    throw new InvalidOperationException("No puedes cerrar el turno: hay mesas con estado OCUPADA/EN_CUENTA.");
            }

            // 3) Cerrar turno (HORA_FIN = ahora)
            using (var up = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "UPDATE TURNOS SET HORA_FIN = CURRENT_TIME WHERE ID_TURNO=@T", conn, tx))
            {
                up.Parameters.Add("@T", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idTurno;
                up.ExecuteNonQuery();
            }

            tx.Commit();
        }



        public static bool ExistenMesasOcupadasOEnCuenta()
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "UTF8");
            using var cmd = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "SELECT COUNT(*) FROM MESAS WHERE ESTADO IN ('OCUPADA','EN_CUENTA')", conn);
            return System.Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }


        public static void LiberarMesa(int idMesa)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "UTF8");
            using var tx = conn.BeginTransaction();

            // Si hay turno abierto, intenta cerrar el MESA_TURNO activo de esta mesa
            int? idTurno = null;
            using (var cmdT = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "SELECT ID_TURNO FROM TURNOS WHERE FECHA=CURRENT_DATE AND HORA_FIN IS NULL ROWS 1", conn, tx))
            {
                var o = cmdT.ExecuteScalar();
                if (o != null && o != DBNull.Value) idTurno = System.Convert.ToInt32(o);
            }

            int? idMesaTurno = null;
            if (idTurno.HasValue)
            {
                using var cmdMT = new FirebirdSql.Data.FirebirdClient.FbCommand(
                    "SELECT ID_MESA_TURNO FROM MESA_TURNO " +
                    "WHERE ID_TURNO=@T AND ID_MESA=@M AND ESTADO IN ('OCUPADA','EN_CUENTA') ROWS 1", conn, tx);
                cmdMT.Parameters.Add("@T", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idTurno.Value;
                cmdMT.Parameters.Add("@M", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idMesa;
                var o = cmdMT.ExecuteScalar();
                if (o != null && o != DBNull.Value) idMesaTurno = System.Convert.ToInt32(o);
            }
            else
            {
                // No hay turno abierto: toma el último MESA_TURNO "abierto" que haya quedado colgado
                using var cmdMTlast = new FirebirdSql.Data.FirebirdClient.FbCommand(
                    "SELECT ID_MESA_TURNO FROM MESA_TURNO " +
                    "WHERE ID_MESA=@M AND ESTADO IN ('OCUPADA','EN_CUENTA') " +
                    "ORDER BY ID_MESA_TURNO DESC ROWS 1", conn, tx);
                cmdMTlast.Parameters.Add("@M", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idMesa;
                var o = cmdMTlast.ExecuteScalar();
                if (o != null && o != DBNull.Value) idMesaTurno = System.Convert.ToInt32(o);
            }

            if (idMesaTurno.HasValue)
            {
                // Cancela cualquier PEDIDO ABIERTO ligado a ese MESA_TURNO (tu trigger los abre en 'ABIERTO') 
                using (var cmdP = new FirebirdSql.Data.FirebirdClient.FbCommand(
                    "UPDATE PEDIDOS SET ESTADO='CANCELADO' WHERE ID_MESA_TURNO=@MT AND ESTADO='ABIERTO'",
                    conn, tx))
                {
                    cmdP.Parameters.Add("@MT", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idMesaTurno.Value;
                    cmdP.ExecuteNonQuery();
                }

                // Cierra MESA_TURNO
                using (var cmdMTclose = new FirebirdSql.Data.FirebirdClient.FbCommand(
                    "UPDATE MESA_TURNO SET ESTADO='CERRADA' WHERE ID_MESA_TURNO=@MT", conn, tx))
                {
                    cmdMTclose.Parameters.Add("@MT", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idMesaTurno.Value;
                    cmdMTclose.ExecuteNonQuery();
                }
            }

            // En todos los casos: dejar la mesa en LIBRE
            using (var cmdM = new FirebirdSql.Data.FirebirdClient.FbCommand(
                "UPDATE MESAS SET ESTADO='LIBRE' WHERE ID_MESA=@M", conn, tx))
            {
                cmdM.Parameters.Add("@M", FirebirdSql.Data.FirebirdClient.FbDbType.Integer).Value = idMesa;
                cmdM.ExecuteNonQuery();
            }

            tx.Commit();
        }


    }
}
