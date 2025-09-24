using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;

namespace PROYECTO_RESIDENCIAS
{
    public static class SaeInventory
    {
        // Genera SALIDAS en MOV_INV_AUX desde las recetas del pedido (g/ml) y luego postea a MINVE01
        public static void ConsumirRecetasYPostear(FbConnection sae, int idPedido, int numAlmacen, int cveConceptoConsumo, string auxCharset = "UTF8")
        {
            // 1) Leer detalle del pedido y expandir recetas a insumos (en g)
            var consumos = new List<(string cveInsumo, decimal gramos, string refer)>();

            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: auxCharset);
            using (var cmd = new FbCommand(@"
SELECT D.CLAVE_ARTICULO_SAE AS CVE_PLATILLO, D.CANTIDAD, D.PESO_GR
FROM PEDIDO_DET D
WHERE D.ID_PEDIDO=@ID AND D.ES_PLATILLO=1", aux))
            {
                cmd.Parameters.Add(new FbParameter("@ID", idPedido));
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    string cvePlat = rd.GetString(0);
                    decimal cant = rd.IsDBNull(1) ? 1m : Convert.ToDecimal(rd[1]);

                    // Obtener receta
                    int idRec = 0;
                    using (var r1 = new FbCommand("SELECT ID_RECETA FROM RECETAS WHERE CLAVE_PLATILLO_SAE=@C", aux))
                    {
                        r1.Parameters.Add(new FbParameter("@C", cvePlat));
                        var o = r1.ExecuteScalar();
                        if (o == null || o == DBNull.Value) continue;
                        idRec = Convert.ToInt32(o);
                    }

                    using var r2 = new FbCommand(@"
SELECT CLAVE_INSUMO_SAE, CANT_GRAMOS FROM RECETA_DET WHERE ID_RECETA=@R", aux);
                    r2.Parameters.Add(new FbParameter("@R", idRec));
                    using var dr = r2.ExecuteReader();
                    while (dr.Read())
                    {
                        string cveIns = dr.GetString(0);
                        decimal g = Convert.ToDecimal(dr[1]);
                        decimal totalG = g * cant;
                        consumos.Add((cveIns, totalG, $"PED-{idPedido}"));
                    }
                }
            }

            // 2) Postear a MINVE01 una salida por insumo (convierte g→kg si UNI_MED='KG')
            using var tx = sae.BeginTransaction();
            foreach (var c in consumos)
            {
                string uniMed = "PZA"; double factorConv = 1.0;
                using (var q = new FbCommand("SELECT COALESCE(UNI_MED,'PZA'), COALESCE(FAC_CONV,1) FROM INVE01 WHERE CVE_ART=@A", sae, tx))
                {
                    q.Parameters.Add(new FbParameter("@A", c.cveInsumo));
                    using var r = q.ExecuteReader();
                    if (r.Read())
                    {
                        uniMed = r.IsDBNull(0) ? "PZA" : r.GetString(0).Trim().ToUpperInvariant();
                        factorConv = r.IsDBNull(1) ? 1.0 : Convert.ToDouble(r[1]);
                    }
                }

                // Cantidad en unidad de venta de SAE:
                // Si insumo se maneja en KG y tenemos gramos, CANT = gramos / 1000, UNI_VENTA='KG', FACTOR_CON=1
                // Si insumo se maneja en PZA, CANT = piezas (aquí gr→pz no aplica). Para líquidos usar ml/L similar.
                decimal cantSae = c.gramos;
                string uniVenta = uniMed;
                double factorCon = 1.0;

                if (uniMed == "KG")
                {
                    cantSae = c.gramos / 1000m;
                    uniVenta = "KG";
                    factorCon = 1.0;
                }
                else if (uniMed == "G")
                {
                    cantSae = c.gramos; // ya está en g
                    uniVenta = "G";
                    factorCon = 1.0;
                }
                else if (uniMed == "L")
                {
                    // si receta está en ml
                    cantSae = c.gramos / 1000m; // 'gramos' aquí representa ml si así definiste la receta
                    uniVenta = "L";
                    factorCon = 1.0;
                }
                else if (uniMed == "ML")
                {
                    cantSae = c.gramos; // ml directo
                    uniVenta = "ML";
                    factorCon = 1.0;
                }
                else
                {
                    // fallback: dejar 1:1
                    cantSae = c.gramos;
                    uniVenta = uniMed;
                    factorCon = 1.0;
                }

                using var ins = new FbCommand(@"
INSERT INTO MINVE01 (
  CVE_ART, ALMACEN, NUM_MOV, CVE_CPTO, FECHA_DOCU, TIPO_DOC, REFER,
  CANT, PRECIO, COSTO, AFEC_COI, TIPO_PROD, FACTOR_CON, FECHAELAB, CVE_FOLIO,
  SIGNO, COSTO_PROM_INI, COSTO_PROM_FIN, COSTO_PROM_GRAL, DESDE_INVE
) VALUES (
  @ART, @ALM, 0, @CPTO, CURRENT_TIMESTAMP, 'O', @REF,
  @CANT, 0, 0, 'N', 'P', @FAC, CURRENT_TIMESTAMP, NULL,
  -1, 0, 0, 0, 'S'
)", sae, tx);

                ins.Parameters.Add(new FbParameter("@ART", c.cveInsumo));
                ins.Parameters.Add(new FbParameter("@ALM", numAlmacen));
                ins.Parameters.Add(new FbParameter("@CPTO", cveConceptoConsumo));
                ins.Parameters.Add(new FbParameter("@REF", c.refer));
                ins.Parameters.Add(new FbParameter("@CANT", cantSae));
                ins.Parameters.Add(new FbParameter("@FAC", factorCon));
                ins.ExecuteNonQuery();
            }
            tx.Commit();
        }
    }
}
