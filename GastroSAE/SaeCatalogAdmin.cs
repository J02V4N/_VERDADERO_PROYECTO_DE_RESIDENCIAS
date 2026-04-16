using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FirebirdSql.Data.FirebirdClient;

namespace GastroSAE
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
            public decimal StockMinimo { get; set; }
            public decimal StockMaximo { get; set; }
        }

        public sealed class UnitProfile
        {
            public string Key { get; init; } = string.Empty;
            public string DisplayName { get; init; } = string.Empty;
            public string UniMed { get; init; } = string.Empty;
            public string UniAlt { get; init; } = string.Empty;
            public decimal FacConv { get; init; }
            public bool UsesScale { get; init; }
        }

        public sealed class InventarioPostItem
        {
            public string Clave { get; set; } = string.Empty;
            public decimal CantidadBase { get; set; }
            public decimal CostoUnitBase { get; set; }
            public string UnidadBase { get; set; } = string.Empty;
            public string UnidadCaptura { get; set; } = string.Empty;
            public decimal CantidadCaptura { get; set; }
        }

        private static readonly UnitProfile[] _unitProfiles =
        {
            new UnitProfile { Key = "kg", DisplayName = "Sólido (kg / gr)", UniMed = "kg", UniAlt = "gr", FacConv = 1000m, UsesScale = false },
            new UnitProfile { Key = "lt", DisplayName = "Líquido (lt / ml)", UniMed = "lt", UniAlt = "ml", FacConv = 1000m, UsesScale = false },
            new UnitProfile { Key = "pz", DisplayName = "Pieza (pz)", UniMed = "pz", UniAlt = "pz", FacConv = 1m, UsesScale = false },
        };

        public static decimal GetSuggestedStockMin(string? unidadBase)
        {
            var profile = ResolveUnitProfile(unidadBase);
            return profile.UniAlt == "pz" ? 20m : 5m;
        }

        public static IReadOnlyList<UnitProfile> GetUnitProfiles() => _unitProfiles;

        public static UnitProfile ResolveUnitProfile(string? unidadBase, string? unidadAlt = null, decimal? facConv = null)
        {
            var baseUnit = NormalizeUnit(unidadBase);
            var altUnit = NormalizeUnit(unidadAlt);
            var factor = facConv.GetValueOrDefault() <= 0 ? 1m : facConv!.Value;

            decimal FactorOrDefault1000() => factor <= 1m ? 1000m : factor;

            if (baseUnit == "pz" || altUnit == "pz")
                return new UnitProfile { Key = "pz", DisplayName = "Pieza (pz)", UniMed = "pz", UniAlt = "pz", FacConv = 1m, UsesScale = false };

            // En SAE, para sólidos y líquidos el patrón correcto es:
            // En esta instalación de SAE, la UI muestra:
            // UNI_MED = unidad de entrada/compra grande (kg/lt)
            // UNI_ALT = unidad de salida/consumo pequeña (gr/ml)
            // FAC_CONV = 1000
            if (baseUnit == "kg" && string.IsNullOrWhiteSpace(altUnit))
                return new UnitProfile { Key = "kg", DisplayName = "Sólido (kg / gr)", UniMed = "kg", UniAlt = "gr", FacConv = FactorOrDefault1000(), UsesScale = false };
            if (baseUnit == "lt" && string.IsNullOrWhiteSpace(altUnit))
                return new UnitProfile { Key = "lt", DisplayName = "Líquido (lt / ml)", UniMed = "lt", UniAlt = "ml", FacConv = FactorOrDefault1000(), UsesScale = false };
            if (baseUnit == "gr" && string.IsNullOrWhiteSpace(altUnit))
                return new UnitProfile { Key = "kg", DisplayName = "Sólido (kg / gr)", UniMed = "kg", UniAlt = "gr", FacConv = FactorOrDefault1000(), UsesScale = false };
            if (baseUnit == "ml" && string.IsNullOrWhiteSpace(altUnit))
                return new UnitProfile { Key = "lt", DisplayName = "Líquido (lt / ml)", UniMed = "lt", UniAlt = "ml", FacConv = FactorOrDefault1000(), UsesScale = false };

            if ((baseUnit == "gr" && altUnit == "kg") || (baseUnit == "kg" && altUnit == "gr"))
                return new UnitProfile { Key = "kg", DisplayName = "Sólido (kg / gr)", UniMed = "kg", UniAlt = "gr", FacConv = FactorOrDefault1000(), UsesScale = false };

            if ((baseUnit == "ml" && altUnit == "lt") || (baseUnit == "lt" && altUnit == "ml"))
                return new UnitProfile { Key = "lt", DisplayName = "Líquido (lt / ml)", UniMed = "lt", UniAlt = "ml", FacConv = FactorOrDefault1000(), UsesScale = false };

            return new UnitProfile
            {
                Key = string.IsNullOrWhiteSpace(baseUnit) ? "custom" : baseUnit,
                DisplayName = string.IsNullOrWhiteSpace(baseUnit) ? "Unidad personalizada" : baseUnit,
                UniMed = string.IsNullOrWhiteSpace(baseUnit) ? "pz" : baseUnit,
                UniAlt = string.IsNullOrWhiteSpace(altUnit) ? (string.IsNullOrWhiteSpace(baseUnit) ? "pz" : baseUnit) : altUnit,
                FacConv = factor,
                UsesScale = false
            };
        }

        public static string NormalizeUnit(string? unit)
            => (unit ?? string.Empty).Trim().ToLowerInvariant();

        public static decimal NormalizeKitQtyForRuntime(string? unidadBase, string? unidadCaptura, decimal factor, decimal cantidadKit)
        {
            factor = factor <= 0m ? 1m : factor;
            var baseUnit = NormalizeUnit(unidadBase);
            var captureUnit = NormalizeUnit(unidadCaptura);

            if (cantidadKit == 0m || string.IsNullOrWhiteSpace(baseUnit) || string.IsNullOrWhiteSpace(captureUnit) || baseUnit == captureUnit)
                return decimal.Round(cantidadKit, 6, MidpointRounding.AwayFromZero);

            bool esParKgGr = baseUnit == "gr" && captureUnit == "kg";
            bool esParLtMl = baseUnit == "ml" && captureUnit == "lt";
            if (!esParKgGr && !esParLtMl)
                return decimal.Round(cantidadKit, 6, MidpointRounding.AwayFromZero);

            // En esta app la receta de trabajo vive en la unidad chica (gr/ml).
            // KITS puede traer históricos capturados en unidad grande (0.025 kg / 0.500 lt).
            // Esos casos se normalizan a gr/ml para que disponibilidad, costo y descuento
            // usen la misma escala que EXIST en INVE/MULT.
            if (cantidadKit > 0m && cantidadKit < 1m)
                return decimal.Round(cantidadKit * factor, 6, MidpointRounding.AwayFromZero);

            return decimal.Round(cantidadKit, 6, MidpointRounding.AwayFromZero);
        }

        public static decimal ConvertCaptureQtyToBase(string? unidadBase, string? unidadCaptura, decimal factor, decimal cantidadCaptura)
        {
            factor = factor <= 0m ? 1m : factor;
            var baseUnit = NormalizeUnit(unidadBase);
            var captureUnit = NormalizeUnit(unidadCaptura);
            if (cantidadCaptura == 0m || string.IsNullOrWhiteSpace(baseUnit) || string.IsNullOrWhiteSpace(captureUnit) || baseUnit == captureUnit)
                return decimal.Round(cantidadCaptura, 6, MidpointRounding.AwayFromZero);

            if ((baseUnit == "gr" && captureUnit == "kg") || (baseUnit == "ml" && captureUnit == "lt"))
                return decimal.Round(cantidadCaptura * factor, 6, MidpointRounding.AwayFromZero);

            if ((baseUnit == "kg" && captureUnit == "gr") || (baseUnit == "lt" && captureUnit == "ml"))
                return decimal.Round(cantidadCaptura / factor, 6, MidpointRounding.AwayFromZero);

            return decimal.Round(cantidadCaptura, 6, MidpointRounding.AwayFromZero);
        }

        public static decimal ConvertCaptureCostToBase(string? unidadBase, string? unidadCaptura, decimal factor, decimal costoCaptura)
        {
            factor = factor <= 0m ? 1m : factor;
            var baseUnit = NormalizeUnit(unidadBase);
            var captureUnit = NormalizeUnit(unidadCaptura);
            if (costoCaptura == 0m || string.IsNullOrWhiteSpace(baseUnit) || string.IsNullOrWhiteSpace(captureUnit) || baseUnit == captureUnit)
                return decimal.Round(costoCaptura, 6, MidpointRounding.AwayFromZero);

            if ((baseUnit == "gr" && captureUnit == "kg") || (baseUnit == "ml" && captureUnit == "lt"))
                return decimal.Round(costoCaptura / factor, 6, MidpointRounding.AwayFromZero);

            if ((baseUnit == "kg" && captureUnit == "gr") || (baseUnit == "lt" && captureUnit == "ml"))
                return decimal.Round(costoCaptura * factor, 6, MidpointRounding.AwayFromZero);

            return decimal.Round(costoCaptura, 6, MidpointRounding.AwayFromZero);
        }

        public static decimal ConvertBaseCostToCapture(string? unidadBase, string? unidadCaptura, decimal factor, decimal costoBase)
        {
            factor = factor <= 0m ? 1m : factor;
            var baseUnit = NormalizeUnit(unidadBase);
            var captureUnit = NormalizeUnit(unidadCaptura);
            if (costoBase == 0m || string.IsNullOrWhiteSpace(baseUnit) || string.IsNullOrWhiteSpace(captureUnit) || baseUnit == captureUnit)
                return decimal.Round(costoBase, 6, MidpointRounding.AwayFromZero);

            if ((baseUnit == "gr" && captureUnit == "kg") || (baseUnit == "ml" && captureUnit == "lt"))
                return decimal.Round(costoBase * factor, 6, MidpointRounding.AwayFromZero);

            if ((baseUnit == "kg" && captureUnit == "gr") || (baseUnit == "lt" && captureUnit == "ml"))
                return decimal.Round(costoBase / factor, 6, MidpointRounding.AwayFromZero);

            return decimal.Round(costoBase, 6, MidpointRounding.AwayFromZero);
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
SELECT CVE_ART, DESCR, UNI_MED, TIPO_ELE, COALESCE(STATUS, 'A') AS STATUS,
       COALESCE(EXIST, 0) AS EXIST, COALESCE(STOCK_MIN, 0) AS STOCK_MIN, COALESCE(STOCK_MAX, 0) AS STOCK_MAX
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
                    Existencia = rd.IsDBNull(5) ? 0m : Convert.ToDecimal(rd.GetValue(5)),
                    StockMinimo = rd.IsDBNull(6) ? 0m : Convert.ToDecimal(rd.GetValue(6)),
                    StockMaximo = rd.IsDBNull(7) ? 0m : Convert.ToDecimal(rd.GetValue(7))
                });
            }
            return list;
        }

        public static void InsertIngrediente(string clave, string descripcion, string unidadBase, decimal stockMin, decimal stockMax)
        {
            clave = (clave ?? string.Empty).Trim();
            descripcion = (descripcion ?? string.Empty).Trim();
            unidadBase = (unidadBase ?? string.Empty).Trim().ToLowerInvariant();
            var profile = ResolveUnitProfile(unidadBase);

            ValidateIngrediente(clave, descripcion, profile.UniAlt, stockMin, stockMax);

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
    @CVE_ART, @DESCR, @LIN_PROD, @CON_SERIE, @UNI_MED, @UNI_EMP, @CTRL_ALM,
    0, @STOCK_MIN, @STOCK_MAX, 'P', 1,
    0, 0, 0, 0, 0,
    'P', @UNI_ALT, @FAC_CONV, 0, 'N', 'N',
    0, 0, 0, 0, 0, 0,
    'N', 'A', 'N', 1, 0, 'C'
)";

            using var cmdIns = new FbCommand(sql, con, tx);
            cmdIns.Parameters.Add("@CVE_ART", FbDbType.VarChar, 16).Value = clave;
            cmdIns.Parameters.Add("@DESCR", FbDbType.VarChar, 40).Value = descripcion;
            cmdIns.Parameters.Add("@LIN_PROD", FbDbType.VarChar, 5).Value = LineInsum;
            cmdIns.Parameters.Add("@CON_SERIE", FbDbType.VarChar, 1).Value = "N";
            cmdIns.Parameters.Add("@UNI_MED", FbDbType.VarChar, 10).Value = profile.UniMed;
            cmdIns.Parameters.Add("@UNI_EMP", FbDbType.Double).Value = 1d;
            cmdIns.Parameters.Add("@CTRL_ALM", FbDbType.VarChar, 1).Value = string.Empty;
            cmdIns.Parameters.Add("@STOCK_MIN", FbDbType.Double).Value = Convert.ToDouble(stockMin);
            cmdIns.Parameters.Add("@STOCK_MAX", FbDbType.Double).Value = Convert.ToDouble(stockMax);
            cmdIns.Parameters.Add("@UNI_ALT", FbDbType.VarChar, 10).Value = profile.UniAlt;
            cmdIns.Parameters.Add("@FAC_CONV", FbDbType.Double).Value = Convert.ToDouble(profile.FacConv);
            cmdIns.ExecuteNonQuery();

            using var cmdUpdExtra = new FbCommand($@"
UPDATE {tINVE}
SET FAC_CONV = @FAC_CONV,
    LIN_PROD = @LIN_PROD,
    CON_SERIE = 'N'
WHERE CVE_ART = @CVE_ART", con, tx);
            cmdUpdExtra.Parameters.Add("@FAC_CONV", FbDbType.Double).Value = Convert.ToDouble(profile.FacConv);
            cmdUpdExtra.Parameters.Add("@LIN_PROD", FbDbType.VarChar, 5).Value = LineInsum;
            cmdUpdExtra.Parameters.Add("@CVE_ART", FbDbType.VarChar, 16).Value = clave;
            cmdUpdExtra.ExecuteNonQuery();

            EnsureArticuloEnAlmacen(con, tx, clave, 1, stockMin, stockMax);
            UpsertPrecioPublico(con, tx, clave, 0m, 1);
            ApplyNativeInveDefaults(con, tx, clave, esKit: false);

            tx.Commit();
        }

        public static void UpdateIngrediente(string clave, string descripcion, string unidadBase, decimal stockMin, decimal stockMax)
        {
            clave = (clave ?? string.Empty).Trim();
            descripcion = (descripcion ?? string.Empty).Trim();
            unidadBase = (unidadBase ?? string.Empty).Trim().ToLowerInvariant();
            var profile = ResolveUnitProfile(unidadBase);

            ValidateIngrediente(clave, descripcion, profile.UniAlt, stockMin, stockMax);

            using var con = SaeDb.GetOpenConnection();
            using var tx = con.BeginTransaction();
            string tINVE = SaeDb.GetTableName(con, "INVE", tx);
            EnsureBaseLines(con, tx);

            using var cmdUpd = new FbCommand($@"
UPDATE {tINVE}
SET DESCR = @DESCR,
    UNI_MED = @UNI_MED,
    UNI_ALT = @UNI_ALT,
    STOCK_MIN = @STOCK_MIN,
    STOCK_MAX = @STOCK_MAX,
    LIN_PROD = @LIN_PROD,
    CON_SERIE = 'N',
    STATUS = 'A',
    TIPO_ELE = 'P',
    TIP_COSTEO = 'P',
    NUM_MON = 1,
    CTRL_ALM = '',
    FCH_ULTCOM = NULL,
    FCH_ULTVTA = @FCH_ULTVTA,
    CVE_OBS = 0,
    CON_LOTE = 'N',
    CON_PEDIMENTO = 'N',
    PESO = 0,
    VOLUMEN = 0,
    CVE_ESQIMPU = 1,
    BLK_CST_EXT = 'N',
    MAN_IEPS = 'N',
    APL_MAN_IMP = 1,
    CUOTA_IEPS = 0,
    APL_MAN_IEPS = 'C'
WHERE CVE_ART = @CVE_ART", con, tx);
            cmdUpd.Parameters.Add("@DESCR", FbDbType.VarChar, 40).Value = descripcion;
            cmdUpd.Parameters.Add("@UNI_MED", FbDbType.VarChar, 10).Value = profile.UniMed;
            cmdUpd.Parameters.Add("@UNI_ALT", FbDbType.VarChar, 10).Value = profile.UniAlt;
            cmdUpd.Parameters.Add("@STOCK_MIN", FbDbType.Double).Value = Convert.ToDouble(stockMin);
            cmdUpd.Parameters.Add("@STOCK_MAX", FbDbType.Double).Value = Convert.ToDouble(stockMax);
            cmdUpd.Parameters.Add("@LIN_PROD", FbDbType.VarChar, 5).Value = LineInsum;
            cmdUpd.Parameters.Add("@FCH_ULTVTA", FbDbType.TimeStamp).Value = DateTime.Today;
            cmdUpd.Parameters.Add("@CVE_ART", FbDbType.VarChar, 16).Value = clave;
            var rows = cmdUpd.ExecuteNonQuery();
            if (rows <= 0)
                throw new Exception("No se encontró el ingrediente en SAE para actualizar.");

            using var cmdUpdExtra = new FbCommand($@"
UPDATE {tINVE}
SET FAC_CONV = @FAC_CONV
WHERE CVE_ART = @CVE_ART", con, tx);
            cmdUpdExtra.Parameters.Add("@FAC_CONV", FbDbType.Double).Value = Convert.ToDouble(profile.FacConv);
            cmdUpdExtra.Parameters.Add("@CVE_ART", FbDbType.VarChar, 16).Value = clave;
            cmdUpdExtra.ExecuteNonQuery();

            EnsureArticuloEnAlmacen(con, tx, clave, 1, stockMin, stockMax);
            UpsertPrecioPublico(con, tx, clave, 0m, 1);
            ApplyNativeInveDefaults(con, tx, clave, esKit: false);

            tx.Commit();
        }

        public static void ApplyNativeInveDefaults(FbConnection con, FbTransaction tx, string clave, bool esKit)
        {
            if (con == null) throw new ArgumentNullException(nameof(con));
            if (tx == null) throw new ArgumentNullException(nameof(tx));
            if (string.IsNullOrWhiteSpace(clave)) throw new ArgumentException("La clave es requerida.", nameof(clave));

            string tINVE = SaeDb.GetTableName(con, "INVE", tx);
            var now = DateTime.Now;
            var hoy = DateTime.Today;
            var updates = new List<string>();

            void AddIfCol(string col, string expr)
            {
                if (ColumnExists(con, tINVE, col, tx)) updates.Add($"{col} = {expr}");
            }

            AddIfCol("CTRL_ALM", "@CTRL_ALM");
            AddIfCol("CVE_OBS", "@CVE_OBS");
            AddIfCol("CVE_ESQIMPU", "@CVE_ESQIMPU");
            AddIfCol("CVE_IMAGEN", "@CVE_IMAGEN");
            AddIfCol("VERSION_SINC", "@VERSION_SINC");
            AddIfCol("VERSION_SINC_FECHA_IMG", "@VERSION_SINC_FECHA_IMG");
            AddIfCol("ANCHO_ML", "@ANCHO_ML");
            AddIfCol("LARGO_ML", "@LARGO_ML");
            AddIfCol("ALTO_ML", "@ALTO_ML");
            AddIfCol("CATEG_ML", "@CATEG_ML");
            AddIfCol("FACT_UNID_CCE", "@FACT_UNID_CCE");
            AddIfCol("COI_SINC", esKit ? "NULL" : "@COI_SINC");
            AddIfCol("FCH_ULTCOM", "NULL");
            AddIfCol("FCH_ULTVTA", "@FCH_ULTVTA");
            AddIfCol("CVE_BITA", "NULL");
            AddIfCol("PREFIJO", "NULL");
            AddIfCol("TALLA", "NULL");
            AddIfCol("COLOR", "NULL");
            AddIfCol("CUENT_CONT", "NULL");
            AddIfCol("UUID", "@UUID");
            AddIfCol("EDO_PUBL_ML", "NULL");
            AddIfCol("CVE_PUBL_ML", "NULL");
            AddIfCol("CONDICION_ML", "NULL");
            AddIfCol("TIPO_PUBL_ML", "NULL");
            AddIfCol("MODO_ENVIO_ML", "NULL");
            AddIfCol("ENVIO_ML", "NULL");
            AddIfCol("CAMPOS_CATEG_ML", "NULL");
            AddIfCol("DISPONIBLE_PUBL", "NULL");
            AddIfCol("CVE_CATE_ML", "NULL");
            AddIfCol("LAST_UPDATE_ML", "NULL");
            AddIfCol("F_CREA_ML", "NULL");
            AddIfCol("IMAGEN_ML", "NULL");
            AddIfCol("EN_CATALOGO", "NULL");
            AddIfCol("ID_CATALOGO", "NULL");
            AddIfCol("TITULO_ML", "NULL");
            AddIfCol("MAT_PELI", "NULL");
            AddIfCol("FRACC_ARANC", "NULL");
            AddIfCol("DESC_ESPECIFICA", "NULL");
            AddIfCol("CVE_PRODSERV", "NULL");
            AddIfCol("CVE_UNIDAD", "NULL");
            AddIfCol("BLK_CST_EXT", "@BLK_CST_EXT");
            AddIfCol("STATUS", "@STATUS");
            AddIfCol("MAN_IEPS", "@MAN_IEPS");
            AddIfCol("APL_MAN_IMP", "@APL_MAN_IMP");
            AddIfCol("CUOTA_IEPS", "@CUOTA_IEPS");
            AddIfCol("APL_MAN_IEPS", "@APL_MAN_IEPS");
            AddIfCol("COMP_X_REC", "@COMP_X_REC");
            AddIfCol("PEND_SURT", "@PEND_SURT");
            AddIfCol("APART", "@APART");
            AddIfCol("CON_LOTE", "@CON_LOTE");
            AddIfCol("CON_PEDIMENTO", "@CON_PEDIMENTO");
            AddIfCol("PESO", "@PESO");
            AddIfCol("VOLUMEN", "@VOLUMEN");
            AddIfCol("TIP_COSTEO", "@TIP_COSTEO");
            AddIfCol("NUM_MON", "@NUM_MON");

            if (updates.Count == 0) return;

            using var cmd = new FbCommand($"UPDATE {tINVE} SET {string.Join(", ", updates)} WHERE CVE_ART = @CVE_ART", con, tx);
            cmd.Parameters.Add("@CTRL_ALM", FbDbType.VarChar, 1).Value = string.Empty;
            cmd.Parameters.Add("@CVE_OBS", FbDbType.Integer).Value = 0;
            cmd.Parameters.Add("@CVE_ESQIMPU", FbDbType.Integer).Value = 1;
            cmd.Parameters.Add("@CVE_IMAGEN", FbDbType.VarChar, 255).Value = string.Empty;
            cmd.Parameters.Add("@VERSION_SINC", FbDbType.TimeStamp).Value = now;
            cmd.Parameters.Add("@VERSION_SINC_FECHA_IMG", FbDbType.TimeStamp).Value = now;
            cmd.Parameters.Add("@ANCHO_ML", FbDbType.Double).Value = 1d;
            cmd.Parameters.Add("@LARGO_ML", FbDbType.Double).Value = 1d;
            cmd.Parameters.Add("@ALTO_ML", FbDbType.Double).Value = 1d;
            cmd.Parameters.Add("@CATEG_ML", FbDbType.VarChar, 255).Value = string.Empty;
            cmd.Parameters.Add("@FACT_UNID_CCE", FbDbType.Double).Value = 1d;
            cmd.Parameters.Add("@COI_SINC", FbDbType.TimeStamp).Value = now;
            cmd.Parameters.Add("@FCH_ULTVTA", FbDbType.TimeStamp).Value = hoy;
            cmd.Parameters.Add("@UUID", FbDbType.VarChar, 50).Value = Guid.NewGuid().ToString().ToUpperInvariant();
            cmd.Parameters.Add("@BLK_CST_EXT", FbDbType.VarChar, 1).Value = "N";
            cmd.Parameters.Add("@STATUS", FbDbType.VarChar, 1).Value = "A";
            cmd.Parameters.Add("@MAN_IEPS", FbDbType.VarChar, 1).Value = "N";
            cmd.Parameters.Add("@APL_MAN_IMP", FbDbType.Integer).Value = 1;
            cmd.Parameters.Add("@CUOTA_IEPS", FbDbType.Double).Value = 0d;
            cmd.Parameters.Add("@APL_MAN_IEPS", FbDbType.VarChar, 1).Value = "C";
            cmd.Parameters.Add("@COMP_X_REC", FbDbType.Double).Value = 0d;
            cmd.Parameters.Add("@PEND_SURT", FbDbType.Double).Value = 0d;
            cmd.Parameters.Add("@APART", FbDbType.Double).Value = 0d;
            cmd.Parameters.Add("@CON_LOTE", FbDbType.VarChar, 1).Value = "N";
            cmd.Parameters.Add("@CON_PEDIMENTO", FbDbType.VarChar, 1).Value = "N";
            cmd.Parameters.Add("@PESO", FbDbType.Double).Value = 0d;
            cmd.Parameters.Add("@VOLUMEN", FbDbType.Double).Value = 0d;
            cmd.Parameters.Add("@TIP_COSTEO", FbDbType.VarChar, 1).Value = "P";
            cmd.Parameters.Add("@NUM_MON", FbDbType.Integer).Value = 1;
            cmd.Parameters.Add("@CVE_ART", FbDbType.VarChar, 16).Value = clave.Trim();
            cmd.ExecuteNonQuery();
        }

        private static bool ColumnExists(FbConnection con, string tableName, string columnName, FbTransaction tx)
        {
            const string sql = @"
SELECT COUNT(*)
FROM RDB$RELATION_FIELDS
WHERE TRIM(RDB$RELATION_NAME) = @T
  AND TRIM(RDB$FIELD_NAME) = @C";
            using var cmd = new FbCommand(sql, con, tx);
            cmd.Parameters.Add("@T", FbDbType.VarChar, 31).Value = tableName.Trim();
            cmd.Parameters.Add("@C", FbDbType.VarChar, 31).Value = columnName.Trim();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
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

        public static void AplicarEntradasInventario(IReadOnlyList<InventarioPostItem> entradas)
        {
            if (entradas == null || entradas.Count == 0)
                throw new Exception("No hay entradas de inventario para guardar.");

            using var con = SaeDb.GetOpenConnection();
            using var tx = con.BeginTransaction();
            string tINVE = SaeDb.GetTableName(con, "INVE", tx);
            string tMINVE = SaeDb.GetTableName(con, "MINVE", tx);
            string tCONM = SaeDb.GetTableName(con, "CONM", tx);
            EnsureBaseLines(con, tx);

            int almacen = ParseWarehouseNumber(GetConfigOrDefault("ALMACEN_DEFAULT", "1")) ?? 1;
            int conceptoEntrada = ResolveInventoryEntryConcept(con, tx, tCONM);
            int nextNumMov = ReadNextInventoryMovementNumber(con, tx, tMINVE);

            foreach (var e in entradas)
            {
                var clave = (e.Clave ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(clave))
                    throw new Exception("Hay una entrada de inventario sin clave de artículo.");
                if (e.CantidadBase <= 0)
                    throw new Exception($"La cantidad para '{clave}' debe ser mayor a cero.");
                if (e.CostoUnitBase < 0)
                    throw new Exception($"El costo para '{clave}' no puede ser negativo.");

                decimal existActual = 0m;
                decimal costoPromActual = 0m;
                string unidadMovimiento = string.IsNullOrWhiteSpace(e.UnidadBase) ? "pz" : e.UnidadBase;

                using (var cmdSel = new FbCommand($@"
SELECT COALESCE(EXIST, 0), COALESCE(COSTO_PROM, 0)
FROM {tINVE}
WHERE CVE_ART = @CVE", con, tx))
                {
                    cmdSel.Parameters.Add("@CVE", FbDbType.VarChar, 16).Value = clave;
                    using var rd = cmdSel.ExecuteReader();
                    if (!rd.Read())
                        throw new Exception($"El artículo '{clave}' ya no existe en SAE.");
                    existActual = rd.IsDBNull(0) ? 0m : Convert.ToDecimal(rd.GetValue(0));
                    costoPromActual = rd.IsDBNull(1) ? 0m : Convert.ToDecimal(rd.GetValue(1));
                }

                var nuevaExist = existActual + e.CantidadBase;
                var nuevaExistAlmacen = nuevaExist;
                decimal nuevoCostoProm;
                if (e.CostoUnitBase > 0)
                {
                    var montoActual = existActual * costoPromActual;
                    var montoEntrada = e.CantidadBase * e.CostoUnitBase;
                    nuevoCostoProm = nuevaExist <= 0 ? e.CostoUnitBase : decimal.Round((montoActual + montoEntrada) / nuevaExist, 6);
                }
                else
                {
                    nuevoCostoProm = costoPromActual;
                }

                int numMov = nextNumMov++;
                InsertInventoryEntryMovement(
                    con,
                    tx,
                    tMINVE,
                    numMov,
                    conceptoEntrada,
                    clave,
                    almacen,
                    e,
                    unidadMovimiento,
                    costoPromActual,
                    nuevoCostoProm,
                    nuevaExist,
                    nuevaExistAlmacen);

                using var cmdUpd = new FbCommand($@"
UPDATE {tINVE}
SET EXIST = @EXIST,
    ULT_COSTO = @ULT_COSTO,
    COSTO_PROM = @COSTO_PROM,
    FCH_ULTCOM = CURRENT_TIMESTAMP,
    COMP_ANL_C = COALESCE(COMP_ANL_C, 0) + @COMP_ANL_C,
    COMP_ANL_M = COALESCE(COMP_ANL_M, 0) + @COMP_ANL_M,
    STATUS = 'A',
    LIN_PROD = COALESCE(NULLIF(TRIM(LIN_PROD), ''), @LIN_PROD),
    CON_SERIE = COALESCE(NULLIF(TRIM(CON_SERIE), ''), 'N')
WHERE CVE_ART = @CVE", con, tx);
                cmdUpd.Parameters.Add("@EXIST", FbDbType.Double).Value = Convert.ToDouble(nuevaExist);
                cmdUpd.Parameters.Add("@ULT_COSTO", FbDbType.Double).Value = Convert.ToDouble(e.CostoUnitBase);
                cmdUpd.Parameters.Add("@COSTO_PROM", FbDbType.Double).Value = Convert.ToDouble(nuevoCostoProm);
                cmdUpd.Parameters.Add("@COMP_ANL_C", FbDbType.Double).Value = Convert.ToDouble(e.CantidadBase);
                cmdUpd.Parameters.Add("@COMP_ANL_M", FbDbType.Double).Value = Convert.ToDouble(decimal.Round(e.CantidadBase * e.CostoUnitBase, 6));
                cmdUpd.Parameters.Add("@LIN_PROD", FbDbType.VarChar, 5).Value = LineInsum;
                cmdUpd.Parameters.Add("@CVE", FbDbType.VarChar, 16).Value = clave;
                var rows = cmdUpd.ExecuteNonQuery();
                if (rows <= 0)
                    throw new Exception($"No se pudo actualizar inventario para '{clave}'.");

                // Multi-almacén desactivado: no se toca MULT##.
            }

            tx.Commit();
        }

        public static void EnsureArticuloEnAlmacen(FbConnection con, FbTransaction tx, string claveArticulo, int almacen = 1, decimal? stockMin = null, decimal? stockMax = null)
        {
            // SAE está operando sin multi-almacén. No se inserta ni actualiza MULT##.
            _ = con;
            _ = tx;
            _ = claveArticulo;
            _ = almacen;
            _ = stockMin;
            _ = stockMax;
        }

        public static void UpsertPrecioPublico(FbConnection con, FbTransaction tx, string claveArticulo, decimal precio, int listaPrecio = 1)
        {
            if (string.IsNullOrWhiteSpace(claveArticulo)) throw new ArgumentException("La clave del artículo es requerida.", nameof(claveArticulo));
            string tPXP = SaeDb.GetTableName(con, "PRECIO_X_PROD", tx);
            var now = DateTime.Now;
            bool hasUuid = ColumnExists(con, tPXP, "UUID", tx);
            bool hasVersionSinc = ColumnExists(con, tPXP, "VERSION_SINC", tx);

            var updParts = new List<string> { "PRECIO = @PRECIO" };
            if (hasUuid)
                updParts.Add("UUID = COALESCE(UUID, @UUID)");
            if (hasVersionSinc)
                updParts.Add("VERSION_SINC = @VERSION_SINC");

            using (var cmdUpd = new FbCommand($@"UPDATE {tPXP} SET {string.Join(", ", updParts)} WHERE CVE_ART = @CVE AND CVE_PRECIO = @LISTA", con, tx))
            {
                cmdUpd.Parameters.Add("@PRECIO", FbDbType.Double).Value = Convert.ToDouble(precio);
                if (hasUuid)
                    cmdUpd.Parameters.Add("@UUID", FbDbType.VarChar, 50).Value = Guid.NewGuid().ToString().ToUpperInvariant();
                if (hasVersionSinc)
                    cmdUpd.Parameters.Add("@VERSION_SINC", FbDbType.TimeStamp).Value = now;
                cmdUpd.Parameters.Add("@CVE", FbDbType.VarChar, 16).Value = claveArticulo.Trim();
                cmdUpd.Parameters.Add("@LISTA", FbDbType.Integer).Value = listaPrecio;
                var rows = cmdUpd.ExecuteNonQuery();
                if (rows > 0) return;
            }

            var cols = new List<string> { "CVE_ART", "CVE_PRECIO", "PRECIO" };
            var vals = new List<string> { "@CVE", "@LISTA", "@PRECIO" };
            if (hasUuid)
            {
                cols.Add("UUID");
                vals.Add("@UUID");
            }
            if (hasVersionSinc)
            {
                cols.Add("VERSION_SINC");
                vals.Add("@VERSION_SINC");
            }

            using var cmdIns = new FbCommand($@"INSERT INTO {tPXP} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)})", con, tx);
            cmdIns.Parameters.Add("@CVE", FbDbType.VarChar, 16).Value = claveArticulo.Trim();
            cmdIns.Parameters.Add("@LISTA", FbDbType.Integer).Value = listaPrecio;
            cmdIns.Parameters.Add("@PRECIO", FbDbType.Double).Value = Convert.ToDouble(precio);
            if (hasUuid)
                cmdIns.Parameters.Add("@UUID", FbDbType.VarChar, 50).Value = Guid.NewGuid().ToString().ToUpperInvariant();
            if (hasVersionSinc)
                cmdIns.Parameters.Add("@VERSION_SINC", FbDbType.TimeStamp).Value = now;
            cmdIns.ExecuteNonQuery();
        }

        private static string GetConfigOrDefault(string key, string defaultValue)
        {
            var value = AuxDbInitializer.GetConfig(key);
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        private static int? ParseWarehouseNumber(string? warehouseText)
        {
            if (int.TryParse((warehouseText ?? string.Empty).Trim(), out var direct))
                return direct;

            var digits = new string((warehouseText ?? string.Empty).Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var parsed))
                return parsed;

            return null;
        }

        private static int ReadNextInventoryMovementNumber(FbConnection con, FbTransaction tx, string minveTable)
        {
            using var cmd = new FbCommand($@"SELECT COALESCE(MAX(NUM_MOV), 0) FROM {minveTable}", con, tx);
            return Convert.ToInt32(cmd.ExecuteScalar()) + 1;
        }

        private static int ResolveInventoryEntryConcept(FbConnection con, FbTransaction tx, string conMTable)
        {
            using (var cmd = new FbCommand($@"
SELECT FIRST 1 CVE_CPTO
FROM {conMTable}
WHERE COALESCE(STATUS, 'A') = 'A'
  AND TIPO_MOV = 'E'
  AND COALESCE(SIGNO, 1) = 1
ORDER BY CVE_CPTO", con, tx))
            {
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result);
            }

            throw new Exception($"No existe un concepto de entrada activo en {conMTable} para registrar el movimiento en MINVE.");
        }

        private static void InsertInventoryEntryMovement(
            FbConnection con,
            FbTransaction tx,
            string minveTable,
            int numMov,
            int conceptoEntrada,
            string claveArticulo,
            int almacen,
            InventarioPostItem entrada,
            string unidadMovimiento,
            decimal costoPromedioInicial,
            decimal costoPromedioFinal,
            decimal nuevaExistenciaGlobal,
            decimal nuevaExistenciaAlmacen)
        {
            decimal precioMovimiento = entrada.CantidadBase > 0m
                ? decimal.Round(entrada.CantidadBase * entrada.CostoUnitBase / entrada.CantidadBase, 6, MidpointRounding.AwayFromZero)
                : 0m;

            var values = new Dictionary<string, object?>
            {
                ["CVE_ART"] = claveArticulo,
                ["ALMACEN"] = almacen,
                ["NUM_MOV"] = numMov,
                ["CVE_CPTO"] = conceptoEntrada,
                ["FECHA_DOCU"] = DateTime.Now.Date,
                ["TIPO_DOC"] = "M",
                ["REFER"] = $"ENTRADA {numMov}",
                ["CLAVE_CLPV"] = string.Empty,
                ["CANT"] = entrada.CantidadBase,
                ["CANT_COST"] = 0m,
                ["PRECIO"] = precioMovimiento,
                ["COSTO"] = entrada.CostoUnitBase,
                ["REG_SERIE"] = 0,
                ["UNI_VENTA"] = string.IsNullOrWhiteSpace(unidadMovimiento) ? "pz" : unidadMovimiento,
                ["E_LTPD"] = 0,
                ["EXIST_G"] = nuevaExistenciaGlobal,
                ["EXISTENCIA"] = nuevaExistenciaAlmacen,
                ["VEND"] = string.Empty,
                ["TIPO_PROD"] = null,
                ["FACTOR_CON"] = 1m,
                ["FECHAELAB"] = DateTime.Now,
                ["CVE_FOLIO"] = numMov.ToString(),
                ["SIGNO"] = 1,
                ["COSTEADO"] = "S",
                ["COSTO_PROM_INI"] = costoPromedioInicial,
                ["COSTO_PROM_FIN"] = costoPromedioFinal,
                ["COSTO_PROM_GRAL"] = costoPromedioFinal,
                ["DESDE_INVE"] = "S",
                ["MOV_ENLAZADO"] = 0
            };

            InsertDynamic(con, tx, minveTable, values);
        }

        private static void InsertDynamic(FbConnection con, FbTransaction tx, string table, IDictionary<string, object?> values)
        {
            var cols = new List<string>();
            var pars = new List<string>();
            using var cmd = new FbCommand { Connection = con, Transaction = tx };
            int i = 0;

            foreach (var kv in values)
            {
                if (kv.Value == null || !ColumnExists(con, table, kv.Key, tx))
                    continue;

                string p = "@P" + i++;
                cols.Add(kv.Key);
                pars.Add(p);
                cmd.Parameters.Add(new FbParameter(p, kv.Value ?? DBNull.Value));
            }

            if (cols.Count == 0)
                throw new Exception($"No hubo columnas válidas para insertar en {table}.");

            cmd.CommandText = $"INSERT INTO {table} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", pars)})";
            cmd.ExecuteNonQuery();
        }

        private static void ValidateIngrediente(string clave, string descripcion, string unidad, decimal stockMin, decimal stockMax)
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
            if (stockMin < 0)
                throw new Exception("El stock mínimo no puede ser menor que cero.");
            if (stockMax < 0)
                throw new Exception("El stock máximo no puede ser menor que cero.");
            if (stockMax > 0 && stockMax < stockMin)
                throw new Exception("El stock máximo no puede ser menor que el stock mínimo.");
        }
    }
}
