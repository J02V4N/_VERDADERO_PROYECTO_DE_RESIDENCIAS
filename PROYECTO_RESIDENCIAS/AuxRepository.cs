using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data;
using static PROYECTO_RESIDENCIAS.Form1;

namespace PROYECTO_RESIDENCIAS
{
    public static class AuxRepository
    {
        // Guarda encabezado + detalle del pedido actual. Devuelve ID_PEDIDO Aux.
        public static int GuardarPedido(Pedido pedido, int idMesaTurno, bool facturarAhora,
                                string metodoPago, string formaPago, decimal importeRecibido,
                                string auxCharset = "UTF8")
        {
            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: auxCharset);
            using var tx = aux.BeginTransaction();

            // Encabezado
            int idPedido;
            using (var cmd = new FbCommand(@"
INSERT INTO PEDIDOS (
    ID_MESA_TURNO, FECHA_HORA, ESTADO,
    SUBTOTAL, IMPUESTO, TOTAL, OBS,
    FACTURAR_AHORA, RFC, RAZON_SOCIAL, USO_CFDI,
    METODO_PAGO, FORMA_PAGO, CLIENTE_CLAVE_SAE
) VALUES (
    @IDMT, CURRENT_TIMESTAMP, 'EN_CUENTA',
    @SUB, @IVA, @TOT, @OBS,
    @FAC, @RFC, @RAZ, @USO,
    @MP, @FP, @CLI
)
RETURNING ID_PEDIDO", aux, tx))
            {
                cmd.Parameters.Add(new FbParameter("@IDMT", FbDbType.Integer) { Value = idMesaTurno });
                cmd.Parameters.Add(new FbParameter("@SUB", FbDbType.Decimal) { Value = pedido.Subtotal, Precision = 18, Scale = 6 });
                cmd.Parameters.Add(new FbParameter("@IVA", FbDbType.Decimal) { Value = pedido.Impuesto, Precision = 18, Scale = 6 });
                cmd.Parameters.Add(new FbParameter("@TOT", FbDbType.Decimal) { Value = pedido.Total, Precision = 18, Scale = 6 });
                cmd.Parameters.Add(new FbParameter("@OBS", FbDbType.VarChar, 255) { Value = pedido.Observaciones ?? "" });
                cmd.Parameters.Add(new FbParameter("@FAC", FbDbType.SmallInt) { Value = facturarAhora ? 1 : 0 });
                cmd.Parameters.Add(new FbParameter("@RFC", FbDbType.VarChar, 13) { Value = pedido.Rfc ?? "" });
                cmd.Parameters.Add(new FbParameter("@RAZ", FbDbType.VarChar, 120) { Value = pedido.RazonSocial ?? "" });
                cmd.Parameters.Add(new FbParameter("@USO", FbDbType.VarChar, 5) { Value = pedido.UsoCfdi ?? "" });
                cmd.Parameters.Add(new FbParameter("@MP", FbDbType.VarChar, 10) { Value = metodoPago ?? "" });
                cmd.Parameters.Add(new FbParameter("@FP", FbDbType.VarChar, 5) { Value = formaPago ?? "" });
                cmd.Parameters.Add(new FbParameter("@CLI", FbDbType.VarChar, 30) { Value = pedido.ClienteClaveSae ?? "" });

                idPedido = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Detalle
            int partida = 1;
            foreach (var d in pedido.Detalles)
            {
                using var cmd = new FbCommand(@"
INSERT INTO PEDIDO_DET (
    ID_PEDIDO, CLAVE_ARTICULO_SAE, ES_PLATILLO,
    CANTIDAD, PESO_GR, PRECIO_UNIT, IMPORTE
) VALUES (
    @ID, @CVE, @ESP, @CANT, @GR, @PU, @IMP
)", aux, tx);

                cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = idPedido });
                cmd.Parameters.Add(new FbParameter("@CVE", FbDbType.VarChar, 30) { Value = d.Clave });
                cmd.Parameters.Add(new FbParameter("@ESP", FbDbType.SmallInt) { Value = d.RequierePeso ? 1 : 0 });
                cmd.Parameters.Add(new FbParameter("@CANT", FbDbType.Decimal) { Value = d.RequierePeso ? 1m : d.Cantidad, Precision = 18, Scale = 6 });
                cmd.Parameters.Add(new FbParameter("@GR", FbDbType.Decimal) { Value = d.RequierePeso ? (object)(d.PesoGr ?? 0m) : DBNull.Value, Precision = 18, Scale = 6 });
                cmd.Parameters.Add(new FbParameter("@PU", FbDbType.Decimal) { Value = d.PrecioUnit, Precision = 18, Scale = 6 });
                cmd.Parameters.Add(new FbParameter("@IMP", FbDbType.Decimal) { Value = d.Importe, Precision = 18, Scale = 6 });

                cmd.ExecuteNonQuery();
                partida++;
            }

            tx.Commit();
            return idPedido;
        }


        public static void TransferirPedidoMesa(int idPedido, int mesaDestino)
        {
            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "UTF8");
            using var cmd = new FbCommand("UPDATE PEDIDOS SET MESA_ID=@M WHERE ID_PEDIDO=@ID", aux);
            cmd.Parameters.Add(new FbParameter("@M", FbDbType.Integer) { Value = mesaDestino });
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = idPedido });
            cmd.ExecuteNonQuery();
        }

        public static void CerrarPedido(int idPedido)
        {
            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "UTF8");
            using var cmd = new FbCommand("UPDATE PEDIDOS SET ESTADO='CERRADA', FECHA_CIERRE=CURRENT_TIMESTAMP WHERE ID_PEDIDO=@ID", aux);
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = idPedido });
            cmd.ExecuteNonQuery();
        }
    }
}
