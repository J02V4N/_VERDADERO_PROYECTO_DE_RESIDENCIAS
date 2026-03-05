using System;
using System.Collections.Generic;
using System.ComponentModel;
using FirebirdSql.Data.FirebirdClient;

namespace PROYECTO_RESIDENCIAS
{
    public static class SaeCatalogAdmin
    {
        public const string LinePrep = "Prep";
        public const string LineInsum = "Insum";

        public sealed class IngredienteDto
        {
            public string Clave { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public string Unidad { get; set; } = string.Empty;
            public string TipoElemento { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public decimal Existencia { get; set; }
        }

        public static void EnsureBaseLines(FbConnection con, FbTransaction tx)
        {
            EnsureLineExists(con, tx, LinePrep, "Lista de Preparados");
            EnsureLineExists(con, tx, LineInsum, "Lista de Insumos");
        }

        public static void EnsureLineExists(FbConnection con, FbTransaction tx, string clave, string descripcion)
        {
            if (con == null) throw new ArgumentNullException(nameof(con));
            if (tx == null) throw new ArgumentNullException(nameof(tx));
            if (string.IsNullOrWhiteSpace(clave)) throw new ArgumentException("La clave de línea es requerida.", nameof(clave));

            string tCLIN = SaeDb.GetTableName(con, "CLIN", tx);

            using (var cmdChk = new FbCommand($@"SELECT COUNT(*) FROM {tCLIN} WHERE CVE_LIN = @C", con, tx))
            {
                cmdChk.Parameters.Add("@C", FbDbType.VarChar, 5).Value = clave.Trim();
                var exists = Convert.ToInt32(cmdChk.ExecuteScalar()) > 0;
                if (exists) return;
            }

            using var cmdIns = new FbCommand($@"
INSERT INTO {tCLIN} (CVE_LIN, DESC_LIN, ESUNGPO, CUENTA_COI, STATUS)
VALUES (@CVE, @DESC, 'N', NULL, 'A')", con, tx);
            cmdIns.Parameters.Add("@CVE", FbDbType.VarChar, 5).Value = clave.Trim();
            cmdIns.Parameters.Add("@DESC", FbDbType.VarChar, 20).Value = (descripcion ?? string.Empty).Trim();
            cmdIns.ExecuteNonQuery();
        }

        public static BindingList<IngredienteDto> ListarIngredientes()
        {
            using var con = SaeDb.GetOpenConnection();
            string tINVE = SaeDb.GetTableName(con, "INVE");
            var sql = $@"
SELECT CVE_ART, DESCR, UNI_MED, TIPO_ELE, COALESCE(STATUS, 'A') AS STATUS, COALESCE(EXIST, 0) AS EXIST
FROM {tINVE}
WHERE COALESCE(LIN_PROD, '') = @LIN
  AND COALESCE(STATUS, 'A') <> 'B'
ORDER BY DESCR, CVE_ART";

            using var cmd = new FbCommand(sql, con);
            cmd.Parameters.Add("@LIN", FbDbType.VarChar, 5).Value = LineInsum;
            using var rd = cmd.ExecuteReader();

            var list = new BindingList<IngredienteDto>();
            while (rd.Read())
            {
                list.Add(new IngredienteDto
                {
                    Clave = rd.IsDBNull(0) ? string.Empty : rd.GetString(0).Trim(),
                    Descripcion = rd.IsDBNull(1) ? string.Empty : rd.GetString(1).Trim(),
                    Unidad = rd.IsDBNull(2) ? string.Empty : rd.GetString(2).Trim(),
                    TipoElemento = rd.IsDBNull(3) ? string.Empty : rd.GetString(3).Trim(),
                    Status = rd.IsDBNull(4) ? string.Empty : rd.GetString(4).Trim(),
                    Existencia = rd.IsDBNull(5) ? 0m : Convert.ToDecimal(rd.GetValue(5))
                });
            }
            return list;
        }

        public static void InsertIngrediente(string clave, string descripcion, string unidad)
        {
            clave = (clave ?? string.Empty).Trim();
            descripcion = (descripcion ?? string.Empty).Trim();
            unidad = (unidad ?? string.Empty).Trim().ToLowerInvariant();

            ValidateIngrediente(clave, descripcion, unidad);

            using var con = SaeDb.GetOpenConnection();
            using var tx = con.BeginTransaction();
            string tINVE = SaeDb.GetTableName(con, "INVE", tx);
            EnsureBaseLines(con, tx);

            using (var cmdChk = new FbCommand($@"SELECT COUNT(*) FROM {tINVE} WHERE CVE_ART = @C", con, tx))
            {
                cmdChk.Parameters.Add("@C", FbDbType.VarChar, 16).Value = clave;
                if (Convert.ToInt32(cmdChk.ExecuteScalar()) > 0)
                    throw new Exception("Ya existe un ingrediente con esa clave en SAE.");
            }

            var sql = $@"
INSERT INTO {tINVE}
(
    CVE_ART, DESCR, LIN_PROD, CON_SERIE, UNI_MED, UNI_EMP, CTRL_ALM,
    TIEM_SURT, STOCK_MIN, STOCK_MAX, TIP_COSTEO, NUM_MON,
    COMP_X_REC, PEND_SURT, EXIST, COSTO_PROM, ULT_COSTO,
    TIPO_ELE, UNI_ALT, FAC_CONV, APART, CON_LOTE, CON_PEDIMENTO,
    PESO, VOLUMEN, VTAS_ANL_C, VTAS_ANL_M, COMP_ANL_C, COMP_ANL_M,
    BLK_CST_EXT, STATUS, MAN_IEPS, APL_MAN_IMP, CUOTA_IEPS, APL_MAN_IEPS
)
VALUES
(
    @CVE_ART, @DESCR, @LIN_PROD, @CON_SERIE, @UNI_MED, @UNI_EMP, NULL,
    0, 0, 0, 'P', 1,
    0, 0, 0, 0, 0,
    'P', @UNI_ALT, 1, 0, 'N', 'N',
    0, 0, 0, 0, 0, 0,
    'N', 'A', 'N', 1, 0, 'C'
)";

            using var cmdIns = new FbCommand(sql, con, tx);
            cmdIns.Parameters.Add("@CVE_ART", FbDbType.VarChar, 16).Value = clave;
            cmdIns.Parameters.Add("@DESCR", FbDbType.VarChar, 40).Value = descripcion;
            cmdIns.Parameters.Add("@LIN_PROD", FbDbType.VarChar, 5).Value = LineInsum;
            cmdIns.Parameters.Add("@CON_SERIE", FbDbType.VarChar, 1).Value = "N";
            cmdIns.Parameters.Add("@UNI_MED", FbDbType.VarChar, 10).Value = unidad;
            cmdIns.Parameters.Add("@UNI_EMP", FbDbType.Double).Value = 1d;
            cmdIns.Parameters.Add("@UNI_ALT", FbDbType.VarChar, 10).Value = unidad;
            cmdIns.ExecuteNonQuery();
            tx.Commit();
        }

        public static void UpdateIngrediente(string clave, string descripcion, string unidad)
        {
            clave = (clave ?? string.Empty).Trim();
            descripcion = (descripcion ?? string.Empty).Trim();
            unidad = (unidad ?? string.Empty).Trim().ToLowerInvariant();

            ValidateIngrediente(clave, descripcion, unidad);

            using var con = SaeDb.GetOpenConnection();
            using var tx = con.BeginTransaction();
            string tINVE = SaeDb.GetTableName(con, "INVE", tx);
            EnsureBaseLines(con, tx);

            using var cmdUpd = new FbCommand($@"
UPDATE {tINVE}
SET DESCR = @DESCR,
    UNI_MED = @UNI_MED,
    UNI_ALT = @UNI_ALT,
    LIN_PROD = @LIN_PROD,
    CON_SERIE = 'N',
    STATUS = 'A',
    TIPO_ELE = 'P'
WHERE CVE_ART = @CVE_ART", con, tx);
            cmdUpd.Parameters.Add("@DESCR", FbDbType.VarChar, 40).Value = descripcion;
            cmdUpd.Parameters.Add("@UNI_MED", FbDbType.VarChar, 10).Value = unidad;
            cmdUpd.Parameters.Add("@UNI_ALT", FbDbType.VarChar, 10).Value = unidad;
            cmdUpd.Parameters.Add("@LIN_PROD", FbDbType.VarChar, 5).Value = LineInsum;
            cmdUpd.Parameters.Add("@CVE_ART", FbDbType.VarChar, 16).Value = clave;
            var rows = cmdUpd.ExecuteNonQuery();
            if (rows <= 0)
                throw new Exception("No se encontró el ingrediente en SAE para actualizar.");
            tx.Commit();
        }

        public static void BajaIngrediente(string clave)
        {
            clave = (clave ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(clave))
                throw new ArgumentException("La clave del ingrediente es requerida.", nameof(clave));

            using var con = SaeDb.GetOpenConnection();
            using var tx = con.BeginTransaction();
            string tINVE = SaeDb.GetTableName(con, "INVE", tx);

            using var cmdUpd = new FbCommand($@"UPDATE {tINVE} SET STATUS = 'B' WHERE CVE_ART = @CVE_ART", con, tx);
            cmdUpd.Parameters.Add("@CVE_ART", FbDbType.VarChar, 16).Value = clave;
            var rows = cmdUpd.ExecuteNonQuery();
            if (rows <= 0)
                throw new Exception("No se encontró el ingrediente en SAE para darlo de baja.");
            tx.Commit();
        }

        private static void ValidateIngrediente(string clave, string descripcion, string unidad)
        {
            if (string.IsNullOrWhiteSpace(clave))
                throw new Exception("La clave del ingrediente es obligatoria.");
            if (clave.Length > 16)
                throw new Exception("La clave del ingrediente no puede exceder 16 caracteres.");
            if (string.IsNullOrWhiteSpace(descripcion))
                throw new Exception("La descripción del ingrediente es obligatoria.");
            if (descripcion.Length > 40)
                throw new Exception("La descripción del ingrediente no puede exceder 40 caracteres.");
            if (string.IsNullOrWhiteSpace(unidad))
                throw new Exception("La unidad del ingrediente es obligatoria.");
            if (unidad.Length > 10)
                throw new Exception("La unidad del ingrediente no puede exceder 10 caracteres.");
        }
    }
}
