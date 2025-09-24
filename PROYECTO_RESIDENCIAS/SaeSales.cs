using FirebirdSql.Data.FirebirdClient;
using System;

namespace PROYECTO_RESIDENCIAS
{
    public static class SaeSales
    {
        /// Obtiene próximo folio por TIP_DOC ('R','F') en FOLIOSC01 y actualiza ULT_DOC.
        public static (string Serie, int Folio, string CveDoc) NextFolio(FbConnection saeConn, string tipDoc)
        {
            using var tx = saeConn.BeginTransaction();
            string serie = null; int next = 0;

            using (var sel = new FbCommand(@"
SELECT SERIE, COALESCE(ULT_DOC, FOLIODESDE-1) AS ULT
FROM FOLIOSC01
WHERE TIP_DOC=@T AND (STATUS IS NULL OR STATUS<>'B')
ORDER BY SERIE
ROWS 1", saeConn, tx))
            {
                sel.Parameters.Add(new FbParameter("@T", tipDoc));
                using var rd = sel.ExecuteReader();
                if (!rd.Read()) throw new InvalidOperationException($"No hay folios para TIP_DOC={tipDoc}");
                serie = rd.GetString(0);
                int ult = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd[1]);
                next = ult + 1;
            }

            using (var upd = new FbCommand(@"
UPDATE FOLIOSC01 SET ULT_DOC=@N, FECH_ULT_DOC=CURRENT_TIMESTAMP
WHERE TIP_DOC=@T AND SERIE=@S", saeConn, tx))
            {
                upd.Parameters.Add(new FbParameter("@N", next));
                upd.Parameters.Add(new FbParameter("@T", tipDoc));
                upd.Parameters.Add(new FbParameter("@S", serie));
                upd.ExecuteNonQuery();
            }

            tx.Commit();
            var cveDoc = $"{serie}{next:00000000}";
            return (serie, next, cveDoc);
        }

        /// Inserta Remisión (FACTR01/PAR_FACTR01). No afecta inventario (ACT_INV='N').
        public static void InsertRemision(FbConnection sae, string cveDoc, string cveCliente, int numAlma,
                                          DateTime fecha, decimal subtotal, decimal impTot1, decimal total,
                                          (string cveArt, decimal cant, decimal precio, string uniVenta)[] partidas,
                                          double ivaPct = 16.0)
        {
            using var tx = sae.BeginTransaction();

            using (var ins = new FbCommand(@"
INSERT INTO FACTR01 (
  TIP_DOC, CVE_DOC, CVE_CLPV, STATUS, DAT_MOSTR, CVE_VEND,
  FECHA_DOC, FECHA_ENT, FECHA_VEN, CAN_TOT, IMP_TOT1,
  DES_TOT, DES_FIN, COM_TOT, CONDICION, CVE_OBS,
  NUM_ALMA, ACT_CXC, ACT_COI, ENLAZADO, TIP_DOC_E,
  NUM_MONED, TIPCAMB, NUM_PAGOS, FECHAELAB, SERIE
) VALUES (
  'R', @DOC, @CLI, 'E', 0, NULL,
  @FEC, @FEC, @FEC, @SUB, @IVA,
  0, 0, 0, '', NULL,
  @ALM, 'S', 'N', 'O', NULL,
  1, 1, 1, CURRENT_TIMESTAMP, NULL
)", sae, tx))
            {
                ins.Parameters.Add(new FbParameter("@DOC", cveDoc));
                ins.Parameters.Add(new FbParameter("@CLI", cveCliente));
                ins.Parameters.Add(new FbParameter("@FEC", fecha));
                ins.Parameters.Add(new FbParameter("@SUB", subtotal));
                ins.Parameters.Add(new FbParameter("@IVA", impTot1));
                ins.Parameters.Add(new FbParameter("@ALM", numAlma));
                ins.ExecuteNonQuery();
            }

            int numPar = 1;
            foreach (var p in partidas)
            {
                using var det = new FbCommand(@"
INSERT INTO PAR_FACTR01 (
  CVE_DOC, NUM_PAR, CVE_ART, CANT, PREC,
  IMPU1, IMPU2, IMPU3, IMPU4, IMPU5, IMPU6, IMPU7, IMPU8,
  TOT_PARTIDA, NUM_ALM, UNI_VENTA, ACT_INV
) VALUES (
  @DOC, @NP, @ART, @CANT, @PREC,
  @IVA, 0, 0, 0, 0, 0, 0, 0,
  @TOT, @ALM, @UNI, 'N'
)", sae, tx);

                det.Parameters.Add(new FbParameter("@DOC", cveDoc));
                det.Parameters.Add(new FbParameter("@NP", numPar++));
                det.Parameters.Add(new FbParameter("@ART", p.cveArt));
                det.Parameters.Add(new FbParameter("@CANT", p.cant));
                det.Parameters.Add(new FbParameter("@PREC", p.PrecioUnit));
                det.Parameters.Add(new FbParameter("@IVA", ivaPct));
                det.Parameters.Add(new FbParameter("@TOT", p.cant * p.PrecioUnit));
                det.Parameters.Add(new FbParameter("@ALM", numAlma));
                det.Parameters.Add(new FbParameter("@UNI", p.uniVenta ?? "PZA"));
                det.ExecuteNonQuery();
            }

            tx.Commit();
        }

        /// Inserta Factura (FACTF01/PAR_FACTF01). Similar a Remisión.
        public static void InsertFactura(FbConnection sae, string cveDoc, string cveCliente, int numAlma,
                                         DateTime fecha, decimal subtotal, decimal impTot1, decimal total,
                                         (string cveArt, decimal cant, decimal precio, string uniVenta)[] partidas,
                                         double ivaPct = 16.0)
        {
            using var tx = sae.BeginTransaction();

            using (var ins = new FbCommand(@"
INSERT INTO FACTF01 (
  TIP_DOC, CVE_DOC, CVE_CLPV, STATUS, DAT_MOSTR, CVE_VEND,
  FECHA_DOC, FECHA_ENT, FECHA_VEN, CAN_TOT, IMP_TOT1,
  DES_TOT, DES_FIN, COM_TOT, CONDICION, CVE_OBS,
  NUM_ALMA, ACT_CXC, ACT_COI, ENLAZADO, TIP_DOC_E,
  NUM_MONED, TIPCAMB, NUM_PAGOS, FECHAELAB, SERIE
) VALUES (
  'F', @DOC, @CLI, 'E', 0, NULL,
  @FEC, @FEC, @FEC, @SUB, @IVA,
  0, 0, 0, '', NULL,
  @ALM, 'S', 'N', 'O', NULL,
  1, 1, 1, CURRENT_TIMESTAMP, NULL
)", sae, tx))
            {
                ins.Parameters.Add(new FbParameter("@DOC", cveDoc));
                ins.Parameters.Add(new FbParameter("@CLI", cveCliente));
                ins.Parameters.Add(new FbParameter("@FEC", fecha));
                ins.Parameters.Add(new FbParameter("@SUB", subtotal));
                ins.Parameters.Add(new FbParameter("@IVA", impTot1));
                ins.Parameters.Add(new FbParameter("@ALM", numAlma));
                ins.ExecuteNonQuery();
            }

            int numPar = 1;
            foreach (var p in partidas)
            {
                using var det = new FbCommand(@"
INSERT INTO PAR_FACTF01 (
  CVE_DOC, NUM_PAR, CVE_ART, CANT, PREC,
  IMPU1, IMPU2, IMPU3, IMPU4, IMPU5, IMPU6, IMPU7, IMPU8,
  TOT_PARTIDA, NUM_ALM, UNI_VENTA, ACT_INV
) VALUES (
  @DOC, @NP, @ART, @CANT, @PREC,
  @IVA, 0, 0, 0, 0, 0, 0, 0,
  @TOT, @ALM, @UNI, 'N'
)", sae, tx);

                det.Parameters.Add(new FbParameter("@DOC", cveDoc));
                det.Parameters.Add(new FbParameter("@NP", numPar++));
                det.Parameters.Add(new FbParameter("@ART", p.cveArt));
                det.Parameters.Add(new FbParameter("@CANT", p.cant));
                det.Parameters.Add(new FbParameter("@PREC", p.PrecioUnit));
                det.Parameters.Add(new FbParameter("@IVA", ivaPct));
                det.Parameters.Add(new FbParameter("@TOT", p.cant * p.PrecioUnit));
                det.Parameters.Add(new FbParameter("@ALM", numAlma));
                det.Parameters.Add(new FbParameter("@UNI", p.uniVenta ?? "PZA"));
                det.ExecuteNonQuery();
            }

            tx.Commit();
        }
    }
}
